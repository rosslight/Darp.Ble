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
>(Host.HciHost host) : IDisposable
    where TCommand : IHciCommand
    where TResponse : IHciEvent<TResponse>
{
#pragma warning disable CA2213 // False positive, the host is not owned by the PacketInFlightHandler
    private readonly Host.HciHost _host = host;
#pragma warning restore CA2213 // Disposable fields should be disposed
    private readonly TaskCompletionSource<TResponse> _completionSource = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private IDisposable? _subscription;
    private readonly bool _waitingForCommandStatus = typeof(TResponse) == typeof(HciCommandStatusEvent);
    private readonly bool _waitingForCommandComplete = typeof(TResponse) == typeof(HciCommandCompleteEvent);

    public async Task<(TResponse Response, Activity? Activity)> QueryAsync(
        TCommand command,
        TimeSpan timeout,
        CancellationToken cancellationToken
    )
    {
        Activity? activity = Logging.StartCommandResponseActivity(command, _host.Device.Address);
        // One deadline on the device's clock for waiting for the command slot and for the answer
        using var timeoutSource = new CancellationTokenSource(timeout, _host.Device.TimeProvider);
        // The deadline ends early when the transport fails: no answer can come then
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, _host.TransportGone);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        try
        {
            int roomReport;
            // Only log the time that is needed to enqueue the command if it actually has to wait.
            Activity? enqueueActivity = !_host.IsCommandSlotFree
                ? Logging.StartEnqueueCommandActivity(TCommand.OpCode, _host.Device.Address)
                : null;
            try
            {
                roomReport = await _host.WaitForCommandSlotAsync(linkedSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (_host.TransportFailure is { } failure && !cancellationToken.IsCancellationRequested)
            {
                throw TransportFailed(failure);
            }
            catch (OperationCanceledException e) when (IsTimeout(timeoutSource, cancellationToken))
            {
                var timeoutException = new TimeoutException("Timeout while waiting for next command to be sent", e);
                enqueueActivity?.AddException(timeoutException);
                enqueueActivity?.SetStatus(ActivityStatusCode.Error, timeoutException.Message);
                throw timeoutException;
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
            // The command slot is taken: whatever happens from here, the next command must get it
            try
            {
                _subscription = _host.Subscribe(this);
                _host.EnqueuePacket(new HciCommandPacket<TCommand>(command), roomReport);
                // Once sent, the controller acts on the command; the answer is awaited whatever the caller does,
                // so the caller learns what the controller did, e.g. that it started to connect
                TResponse result = await _completionSource.Task.WaitAsync(deadline.Token).ConfigureAwait(false);
                activity?.SetTag("Response.OpCode", $"{TResponse.EventCode.ToString().ToUpperInvariant()}_EVENT");
                activity?.SetDeconstructedTags("Response", result, orderEntries: true);
                return (result, activity);
            }
            catch (OperationCanceledException) when (_host.TransportFailure is { } failure)
            {
                throw TransportFailed(failure);
            }
            catch (OperationCanceledException e) when (timeoutSource.IsCancellationRequested)
            {
                throw new TimeoutException($"The controller did not answer {TCommand.OpCode} within {timeout}", e);
            }
            finally
            {
                _host.ReleaseCommandSlot();
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

    private static HciException TransportFailed(Exception failure) =>
        new($"The HCI transport failed while waiting for {TCommand.OpCode}", failure);

    private static bool IsTimeout(CancellationTokenSource timeoutSource, CancellationToken cancellationToken) =>
        timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested;

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
    private void OnCommandComplete(in HciCommandCompleteEvent message)
    {
        // Another command's answer, or an event that only gives room for commands (opcode 0x0000).
        // An answer names only the opcode: when a command timed out and a room report let the next one with the same
        // opcode go, the late answer completes that one. Only a controller that already broke the timeout gets here
        if (!_waitingForCommandComplete || message.CommandOpCode != TCommand.OpCode)
            return;
        HciCommandCompleteEvent complete = message;
        _completionSource.TrySetResult(Unsafe.As<HciCommandCompleteEvent, TResponse>(ref complete));
    }

    [MessageSink]
    private void OnCommandStatus(in HciCommandStatusEvent message)
    {
        if (message.CommandOpCode != TCommand.OpCode)
            return;
        // Command status events reach this sink only, so a query for the status itself completes here
        if (_waitingForCommandStatus)
        {
            HciCommandStatusEvent status = message;
            _completionSource.TrySetResult(Unsafe.As<HciCommandStatusEvent, TResponse>(ref status));
            return;
        }
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
