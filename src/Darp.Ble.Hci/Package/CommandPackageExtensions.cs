using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Exceptions;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Reactive;

namespace Darp.Ble.Hci.Package;

/// <summary> Extensions regarding command packets </summary>
public static class CommandPackageExtensions
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private static void OnNextEventPacket<TCommand>(this IRefObserver<HciEventPacket> observer, HciEventPacket package)
        where TCommand : IHciCommand
    {
        try
        {
            if (HciEventPacket.TryWithData(package, out HciEventPacket<HciCommandStatusEvent>? statusPackage))
            {
                if (
                    statusPackage.Data.CommandOpCode == TCommand.OpCode
                    && statusPackage.Data.Status is not HciCommandStatus.Success
                )
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

    private static IRefObservable<HciEventPacket> QueryCommand<TCommand>(this HciHost hciHost, TCommand command)
        where TCommand : IHciCommand
    {
        return RefObservable.Create<HciEventPacket>(observer =>
        {
            IDisposable disposable = hciHost.WhenHciEventReceived.Subscribe(
                observer.OnNextEventPacket<TCommand>,
                observer.OnError,
                observer.OnCompleted
            );
            var commandPacket = new HciCommandPacket<TCommand>(command);
            hciHost.Logger.LogStartQuery(commandPacket);
            hciHost.EnqueuePacket(commandPacket);
            return disposable;
        });
    }

    /// <summary> Query a command expecting a <see cref="HciCommandCompleteEvent{TParameters}"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <typeparam name="TResponse"> The type of the parameters of the response packet </typeparam>
    /// <returns> The parameters of the response packet </returns>
    public static Task<TResponse> QueryCommandCompletionAsync<TCommand, TResponse>(
        this HciHost hciHost,
        CancellationToken cancellationToken = default
    )
        where TCommand : unmanaged, IHciCommand
        where TResponse : unmanaged, IBinaryReadable<TResponse>
    {
        return hciHost.QueryCommandCompletionAsync<TCommand, TResponse>(default, timeout: null, cancellationToken);
    }

    /// <summary> Query a command expecting a <see cref="HciCommandCompleteEvent{TParameters}"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="command"> The command to be sent </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <typeparam name="TParameters"> The type of the parameters of the response packet </typeparam>
    /// <returns> The parameters of the response packet </returns>
    public static Task<TParameters> QueryCommandCompletionAsync<TCommand, TParameters>(
        this HciHost hciHost,
        TCommand command,
        CancellationToken cancellationToken = default
    )
        where TCommand : IHciCommand
        where TParameters : unmanaged, IBinaryReadable<TParameters>
    {
        return hciHost.QueryCommandCompletionAsync<TCommand, TParameters>(command, timeout: null, cancellationToken);
    }

    /// <summary> Query a command expecting a <see cref="HciCommandCompleteEvent{TParameters}"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="command"> The command to be sent </param>
    /// <param name="timeout"> The timeout waiting for the response </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <typeparam name="TParameters"> The type of the parameters of the response packet </typeparam>
    /// <returns> The parameters of the response packet </returns>
    [OverloadResolutionPriority(1)]
    public static async Task<TParameters> QueryCommandCompletionAsync<TCommand, TParameters>(
        this HciHost hciHost,
        TCommand command,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
        where TCommand : IHciCommand
        where TParameters : unmanaged, IBinaryReadable<TParameters>
    {
        timeout ??= TimeSpan.FromSeconds(10);
        HciEventPacket<HciCommandCompleteEvent<TParameters>> packet = await RefObservable
            .Create<HciEventPacket>(observer =>
            {
                return hciHost
                    .QueryCommand(command)
                    .Subscribe(
                        next =>
                        {
                            if (
                                next.EventCode == HciCommandStatusEvent.EventCode
                                && HciEventPacket.TryWithData(
                                    next,
                                    out HciEventPacket<HciCommandStatusEvent>? statusResult
                                )
                                && statusResult.Data.CommandOpCode == TCommand.OpCode
                            )
                            {
                                observer.OnError(
                                    new HciException($"Command failed with status {statusResult.Data.Status}")
                                );
                                return;
                            }
                            observer.OnNext(next);
                        },
                        observer.OnError,
                        observer.OnCompleted
                    );
            })
            .SelectWhereEvent<HciCommandCompleteEvent<TParameters>>()
            .Where(x => x.Data.CommandOpCode == TCommand.OpCode)
            .Do(
                completePacket => hciHost.Logger.LogQueryCompleted(command, completePacket.EventCode, completePacket),
                exception => hciHost.Logger.LogQueryWithException(exception, command, exception.Message)
            )
            .AsObservable()
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
    public static IRefObservable<HciEventPacket<HciCommandStatusEvent>> QueryCommandStatus<TCommand>(
        this HciHost hciHost,
        TCommand command = default,
        TimeSpan? timeout = null
    )
        where TCommand : unmanaged, IHciCommand
    {
        timeout ??= TimeSpan.FromSeconds(10);
        try
        {
            return hciHost
                .QueryCommand(command)
                .SelectWhereEvent<HciCommandStatusEvent>()
                .Where(x => x.Data.CommandOpCode == TCommand.OpCode)
                .Do(
                    statusPacket =>
                        hciHost.Logger.LogQueryStarted(
                            command,
                            statusPacket.Data.Status,
                            statusPacket.EventCode,
                            statusPacket
                        ),
                    exception => hciHost.Logger.LogQueryWithException(exception, command, exception.Message)
                )
                .First()
                .AsObservable()
                .Timeout(timeout.Value)
                .AsRefObservable();
        }
        catch (Exception e)
        {
            return RefObservable.Throw<HciEventPacket<HciCommandStatusEvent>>(e);
        }
    }

    /// <summary> Query a command expecting a <see cref="HciCommandStatusEvent"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="command"> The command to be sent </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <returns> The command status </returns>
    public static async Task<HciCommandStatus> QueryCommandStatusAsync<TCommand>(
        this HciHost hciHost,
        TCommand command = default,
        CancellationToken cancellationToken = default
    )
        where TCommand : unmanaged, IHciCommand
    {
        HciEventPacket<HciCommandStatusEvent> packet = await hciHost
            .QueryCommandStatus(command)
            .AsObservable()
            .ToTask(cancellationToken)
            .ConfigureAwait(false);
        return packet.Data.Status;
    }
}
