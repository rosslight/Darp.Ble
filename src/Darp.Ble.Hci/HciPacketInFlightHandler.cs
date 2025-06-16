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
> : IDisposable
    where TCommand : IHciCommand
    where TResponse : IHciEvent<TResponse>
{
    private readonly HciHost _host;
    private readonly SemaphoreSlim _packetInFlightSemaphore;
    private readonly TaskCompletionSource<TResponse> _completionSource = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private readonly IDisposable _subscription;
    private readonly bool _waitingForCommandStatus;

    public HciPacketInFlightHandler(HciHost host, SemaphoreSlim packetInFlightSemaphore)
    {
        _host = host;
        _packetInFlightSemaphore = packetInFlightSemaphore;
        _subscription = _host.Subscribe(this);
        _waitingForCommandStatus = typeof(TResponse) == typeof(HciCommandStatusEvent);
    }

    public async Task<(TResponse Response, Activity? Activity)> QueryAsync(
        TCommand command,
        TimeSpan timeout,
        CancellationToken cancellationToken
    )
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        Activity? activity = Logging.StartCommandResponseActivity(command, _host.Address);
        try
        {
            Activity? sendActivity = Logging.StartSendCommandActivity(TCommand.OpCode, _host.Address);
            try
            {
                bool enteredSlim = await _packetInFlightSemaphore
                    .WaitAsync(timeout, cancellationToken)
                    .ConfigureAwait(false);
                if (!enteredSlim)
                {
                    throw new TimeoutException("Timeout while waiting for next command to be sent");
                }
                var packet = new HciCommandPacket<TCommand>(command);
                _host.EnqueuePacket(packet);
            }
            catch (Exception e)
            {
                sendActivity?.AddException(e);
                sendActivity?.SetStatus(ActivityStatusCode.Error, e.Message);
                throw;
            }
            finally
            {
                sendActivity?.Dispose();
            }

            Activity? receiveActivity = Logging.StartWaitForEventActivity(TResponse.EventCode, _host.Address);
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
            catch (Exception e)
            {
                receiveActivity?.AddException(e);
                receiveActivity?.SetStatus(ActivityStatusCode.Error, e.Message);
                throw;
            }
            finally
            {
                receiveActivity?.Dispose();
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
        _packetInFlightSemaphore.Release();
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
        _packetInFlightSemaphore.Release();
    }

    public void Dispose() => _subscription.Dispose();
}
