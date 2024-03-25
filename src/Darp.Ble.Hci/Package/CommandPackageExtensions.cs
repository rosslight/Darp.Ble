using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Package;

public readonly record struct AttReadResult(AttOpCode OpCode, byte[] Pdu);

public static class CommandPackageExtensions
{
    public static async Task<TParameters> QueryCommandCompletionAsync<TCommand, TParameters>(this HciHost hciHost,
        TCommand command = default, TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TParameters : unmanaged, IDecodable<TParameters>
        where TCommand : unmanaged, IHciCommand<TCommand>
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
                        observer.OnError(new Exception($"Command failed with status {statusResult.Data.Status}"));
                        return;
                    }
                    observer.OnNext(next);
                }, observer.OnError, observer.OnCompleted);
            })
            .SelectWhereEvent<HciCommandCompleteEvent<TParameters>>()
            .Where(x => x.Data.CommandOpCode == TCommand.OpCode)
            .Do(completePacket =>
            {
                // hciHost.Logger.Verbose(
                //     "HciHost: Query {@Command} from client completed successfully: Received {EventCode} {@Packet}",
                //     command, completePacket.EventCode, completePacket);
            }, exception =>
            {
                // hciHost.Logger.Error(exception, "HciHost: Query {@Command} from client failed because of {Reason}",
                //     command, exception.Message);
            })
            .FirstAsync()
            .Timeout(timeout.Value)
            .ToTask(cancellationToken);
        return packet.Data.ReturnParameters;
    }

    public static IObservable<HciEventPacket<HciCommandStatusEvent>> QueryCommandStatus<TCommand>(this HciHost hciHost,
        TCommand command = default, TimeSpan? timeout = null)
        where TCommand : unmanaged, IHciCommand<TCommand>
    {
        timeout ??= TimeSpan.FromSeconds(10);
        return Observable
            .Create<HciEventPacket<HciCommandStatusEvent>>(observer => hciHost
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
            .Do(statusPacket =>
            {
                // hciHost.Logger.Verbose(
                //     "HciHost: Query {@Command} from client started with status {Status}: Received {EventCode} {@Packet}",
                //     command, statusPacket.Data.Status, statusPacket.EventCode, statusPacket);
            }, exception =>
            {
                // hciHost.Logger.Error(exception, "HciHost: Query {@Command} from client failed because of {Reason}",
                //     command, exception.Message);
            })
            .FirstAsync()
            .Timeout(timeout.Value);
    }

    public static async Task<HciCommandStatus> QueryCommandStatusAsync<TCommand>(this HciHost hciHost,
        TCommand command = default,
        CancellationToken cancellationToken = default)
        where TCommand : unmanaged, IHciCommand<TCommand>
    {
        HciEventPacket<HciCommandStatusEvent> packet = await hciHost.QueryCommandStatus(command).ToTask(cancellationToken);
        return packet.Data.Status;
    }

    public static IObservable<HciEventPacket> QueryCommand<TCommand>(this HciHost hciHost,
        TCommand command = default)
        where TCommand : unmanaged, IHciCommand<TCommand>
    {
        return Observable.Create<HciEventPacket>(observer =>
        {
            //hciHost.Logger.Verbose("Starting query of {@Command}", command);
            IDisposable disposable = hciHost.WhenHciEventPackageReceived
                .Subscribe(package =>
                {
                    try
                    {
                        if (HciEventPacket.TryWithData(package, out HciEventPacket<HciCommandStatusEvent>? statusPackage))
                        {
                            if (statusPackage.Data.CommandOpCode == TCommand.OpCode
                                && statusPackage.Data.Status is not HciCommandStatus.Success)
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
                }, observer.OnError, observer.OnCompleted);
            hciHost.EnqueuePacket(new HciCommandPacket<TCommand>(command));
            return disposable;
        });
    }
}