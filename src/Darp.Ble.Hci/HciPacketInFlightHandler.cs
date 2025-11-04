using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Darp.Ble.Hci.Exceptions;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Darp.Utils.Messaging;

namespace Darp.Ble.Hci;

internal sealed partial class HciPacketInFlightHandler<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TCommand,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TResponse
>(Host.HciHost host, SemaphoreSlim packetInFlightSemaphore) : IDisposable
    where TCommand : IHciCommand
    where TResponse : IHciEvent<TResponse>
{
#pragma warning disable CA2213 // False positive, these fields are not owned by the PacketInFlightHandler
    private readonly Host.HciHost _host = host;
    private readonly SemaphoreSlim _packetInFlightSemaphore = packetInFlightSemaphore;
#pragma warning restore CA2213 // Disposable fields should be disposed
    private readonly TaskCompletionSource<TResponse> _completionSource = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private IDisposable? _subscription;
    private readonly bool _waitingForCommandStatus = typeof(TResponse) == typeof(HciCommandStatusEvent);

    public async Task<(TResponse Response, Activity? Activity)> QueryAsync(
        TCommand command,
        TimeSpan timeout,
        CancellationToken cancellationToken
    )
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        Activity? activity = Logging.StartCommandResponseActivity(command, _host.Device.Address);
        try
        {
            // Only log the time that is needed to enqueue the command if it actually has to wait.
            Activity? enqueueActivity = _packetInFlightSemaphore.CurrentCount is 0
                ? Logging.StartEnqueueCommandActivity(TCommand.OpCode, _host.Device.Address)
                : null;
            try
            {
                bool enteredSlim = await _packetInFlightSemaphore
                    .WaitAsync(timeout, cancellationToken)
                    .ConfigureAwait(false);
                if (!enteredSlim)
                {
                    throw new TimeoutException("Timeout while waiting for next command to be sent");
                }
                _subscription = _host.Subscribe(this);
                var packet = new HciCommandPacket<TCommand>(command);
                _host.EnqueuePacket(packet);
            }
            catch (Exception e)
            {
                enqueueActivity?.AddException(e);
                enqueueActivity?.SetStatus(ActivityStatusCode.Error, e.Message);
                throw;
            }
            finally
            {
                enqueueActivity?.Dispose();
            }
            try
            {
                TimeSpan waitTime = DateTimeOffset.UtcNow - startTime;
                TimeSpan timeoutAfterWait = timeout > waitTime ? timeout - waitTime : TimeSpan.Zero;
                TResponse result = await _completionSource
                    .Task.WaitAsync(timeoutAfterWait, cancellationToken)
                    .ConfigureAwait(false);
                activity?.SetTag("Response.OpCode", $"{TResponse.EventCode.ToString().ToUpperInvariant()}_EVENT");
                activity?.SetDeconstructedTags("Response", result, orderEntries: true);
                return (result, activity);
            }
            finally
            {
                // Release the InFlight semaphore at the end of the query to ensure the next command can continue
                // TODO: What do we do if a response is received after a timeout was hit? Will this trigger the next query?
                _packetInFlightSemaphore.Release();
            }
        }
        catch (Exception e)
        {
            activity?.AddException(e);
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);
            activity?.Dispose();
            throw;
        }
    }

    [MessageSink]
    private void Publish<T>(in T message)
        where T : allows ref struct
    {
        if (typeof(T) != typeof(TResponse))
            return;
        T messageToCopy = message;
        TResponse response = Unsafe.As<T, TResponse>(ref messageToCopy);
        _completionSource.TrySetResult(response);
    }

    [MessageSink]
    private void OnCommandStatus(in HciCommandStatusEvent message)
    {
        if (_waitingForCommandStatus)
            return;
        if (message.CommandOpCode != TCommand.OpCode)
            return;
        if (message.Status is HciCommandStatus.Success)
            return;
        _completionSource.TrySetException(new HciException($"Command failed with status {message.Status}"));
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _completionSource.TrySetCanceled();
    }
}
