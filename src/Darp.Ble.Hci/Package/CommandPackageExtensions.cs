using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Hci.Exceptions;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Package;

/// <summary> Extensions regarding command packets </summary>
public static class CommandPackageExtensions
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private static void OnNextEventPacket<TCommand>(this IObserver<HciEventPacket> observer, HciEventPacket package)
        where TCommand : unmanaged, IHciCommand<TCommand>
    {
        try
        {
            if (HciEventPacket.TryWithData(package, out HciEventPacket<HciCommandStatusEvent>? statusPackage))
            {
                if (statusPackage.Data.CommandOpCode == TCommand.OpCode && statusPackage.Data.Status is not HciCommandStatus.Success)
                {
                    observer.OnError(new HciEventFailedException(statusPackage));
                    return;
                }
            }
            observer.OnNext(package);
        }
        catch (Exception e)
        {
            observer.OnError(e);
        }
    }

    private static IObservable<HciEventPacket> QueryCommand<TCommand>(this HciHost hciHost,
        TCommand command = default)
        where TCommand : unmanaged, IHciCommand<TCommand>
    {
        return Observable.Create<HciEventPacket>(observer =>
        {
            IDisposable disposable = hciHost.WhenHciEventPackageReceived
                .Subscribe(observer.OnNextEventPacket<TCommand>, observer.OnError, observer.OnCompleted);
            var commandPacket = new HciCommandPacket<TCommand>(command);
            hciHost.Logger?.LogStartQuery(commandPacket);
            hciHost.EnqueuePacket(commandPacket);
            return disposable;
        });
    }

    /// <summary> Query a command expecting a <see cref="HciCommandCompleteEvent{TParameters}"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="command"> The command to be sent </param>
    /// <param name="timeout"> The timeout waiting for the response </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <typeparam name="TParameters"> The type of the parameters of the response packet </typeparam>
    /// <returns> The parameters of the response packet </returns>
    public static async Task<TParameters> QueryCommandCompletionAsync<TCommand, TParameters>(this HciHost hciHost,
        TCommand command = default, TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TCommand : unmanaged, IHciCommand<TCommand>
        where TParameters : unmanaged, IDecodable<TParameters>
    {
        timeout ??= TimeSpan.FromSeconds(10);
        HciEventPacket<HciCommandCompleteEvent<TParameters>> packet = await Observable
            .Create<HciEventPacket>(observer =>
            {
                return hciHost.QueryCommand(command).Subscribe(next =>
                {
                    if (next.EventCode == HciCommandStatusEvent.EventCode
                        && HciEventPacket.TryWithData(next, out HciEventPacket<HciCommandStatusEvent>? statusResult)
                        && statusResult.Data.CommandOpCode == TCommand.OpCode)
                    {
                        observer.OnError(new HciException($"Command failed with status {statusResult.Data.Status}"));
                        return;
                    }
                    observer.OnNext(next);
                }, observer.OnError, observer.OnCompleted);
            })
            .SelectWhereEvent<HciCommandCompleteEvent<TParameters>>()
            .Where(x => x.Data.CommandOpCode == TCommand.OpCode)
            .Do(completePacket => hciHost.Logger?.LogQueryCompleted(command, completePacket.EventCode, completePacket),
                exception => hciHost.Logger?.LogQueryWithException(exception, command, exception.Message))
            .FirstAsync()
            .Timeout(timeout.Value)
            .ToTask(cancellationToken)
            .ConfigureAwait(false);
        return packet.Data.ReturnParameters;
    }

    /// <summary> Query a command expecting a <see cref="HciCommandStatusEvent"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="command"> The command to be sent </param>
    /// <param name="timeout"> The timeout waiting for the response </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <returns> An observable which gives an event about the status </returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public static IObservable<HciEventPacket<HciCommandStatusEvent>> QueryCommandStatus<TCommand>(this HciHost hciHost,
        TCommand command = default, TimeSpan? timeout = null)
        where TCommand : unmanaged, IHciCommand<TCommand>
    {
        timeout ??= TimeSpan.FromSeconds(10);
        try
        {
            return Observable.Create<HciEventPacket<HciCommandStatusEvent>>(observer => hciHost
                    .QueryCommand(command)
                    .SelectWhereEvent<HciCommandStatusEvent>()
                    .Subscribe(statusPackage =>
                    {
                        try
                        {
                            if (statusPackage.Data.CommandOpCode != TCommand.OpCode) return;
                            observer.OnNext(statusPackage);
                        }
                        catch (Exception e)
                        {
                            observer.OnError(e);
                        }
                    }, observer.OnError, observer.OnCompleted))
                .Do(
                    statusPacket => hciHost.Logger?.LogQueryStarted(command, statusPacket.Data.Status,
                        statusPacket.EventCode, statusPacket),
                    exception => hciHost.Logger?.LogQueryWithException(exception, command, exception.Message))
                .FirstAsync()
                .Timeout(timeout.Value);
        }
        catch (Exception e)
        {
            return Observable.Throw<HciEventPacket<HciCommandStatusEvent>>(e);
        }
    }

    /// <summary> Query a command expecting a <see cref="HciCommandStatusEvent"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="command"> The command to be sent </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <returns> The command status </returns>
    public static async Task<HciCommandStatus> QueryCommandStatusAsync<TCommand>(this HciHost hciHost,
        TCommand command = default,
        CancellationToken cancellationToken = default)
        where TCommand : unmanaged, IHciCommand<TCommand>
    {
        HciEventPacket<HciCommandStatusEvent> packet = await hciHost.QueryCommandStatus(command)
            .ToTask(cancellationToken)
            .ConfigureAwait(false);
        return packet.Data.Status;
    }
}