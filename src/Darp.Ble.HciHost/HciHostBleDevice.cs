using Darp.Ble.Data;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Hci.Transport;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

/// <summary> Provides windows specific implementation of a ble device </summary>
public sealed class HciHostBleDevice(string port, string name, ILogger? logger) : BleDevice(logger)
{
    public Hci.HciHost Host { get; } = new(new H4TransportLayer(port, logger: logger), logger: logger);

    public override string Name { get; } = name;

    /// <param name="cancellationToken"></param>
    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        Host.Initialize();
        await Host.QueryCommandCompletionAsync<HciResetCommand, HciResetResult>(cancellationToken: cancellationToken);
        //await Host.QueryCommandCompletionAsync<HciReadLocalSupportedCommandsCommand, HciReadLocalSupportedCommandsResult>();
        await Host.QueryCommandCompletionAsync<HciSetEventMaskCommand, HciSetEventMaskResult>(new HciSetEventMaskCommand((EventMask)0x3fffffffffffffff), cancellationToken: cancellationToken);
        await Host.QueryCommandCompletionAsync<HciLeSetEventMaskCommand, HciLeSetEventMaskResult>(new HciLeSetEventMaskCommand((LeEventMask)0xf0ffff), cancellationToken: cancellationToken);
        await Host.QueryCommandCompletionAsync<HciLeSetRandomAddressCommand, HciLeSetRandomAddressResult>(new HciLeSetRandomAddressCommand(0xF0F1F2F3F4F5), cancellationToken: cancellationToken);
        Observer = new HciHostBleObserver(this, Logger);
        Central = new HciHostBleCentral(this, Logger);
        return InitializeResult.Success;
    }

    /// <inheritdoc />
    public override string Identifier => "Darp.Ble.HciHost";

    protected override void DisposeCore()
    {
        Host.Dispose();
        base.DisposeCore();
    }
}