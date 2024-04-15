using Darp.Ble.Data;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Hci.Transport;
using Darp.Ble.Implementation;

namespace Darp.Ble.HciHost;

/// <summary> Provides windows specific implementation of a ble device </summary>
public sealed class HciHostBleDevice(string port) : IPlatformSpecificBleDevice
{
    public Hci.HciHost Host { get; } = new(new H4TransportLayer(port, logger: null), logger: null);

    public string Name => port;

    /// <param name="cancellationToken"></param>
    /// <inheritdoc />
    public async Task<InitializeResult> InitializeAsync(CancellationToken cancellationToken)
    {
        Host.Initialize();
        await Host.QueryCommandCompletionAsync<HciResetCommand, HciResetResult>(cancellationToken: cancellationToken);
        //await Host.QueryCommandCompletionAsync<HciReadLocalSupportedCommandsCommand, HciReadLocalSupportedCommandsResult>();
        await Host.QueryCommandCompletionAsync<HciSetEventMaskCommand, HciSetEventMaskResult>(new HciSetEventMaskCommand((EventMask)0x3fffffffffffffff), cancellationToken: cancellationToken);
        await Host.QueryCommandCompletionAsync<HciLeSetEventMaskCommand, HciLeSetEventMaskResult>(new HciLeSetEventMaskCommand((LeEventMask)0xf0ffff), cancellationToken: cancellationToken);
        await Host.QueryCommandCompletionAsync<HciLeSetRandomAddressCommand, HciLeSetRandomAddressResult>(new HciLeSetRandomAddressCommand(0xF0F1F2F3F4F5), cancellationToken: cancellationToken);
        Observer = new HciHostBleObserver(this);
        return InitializeResult.Success;
    }

    /// <inheritdoc />
    public IPlatformSpecificBleObserver? Observer { get; private set; }
    /// <inheritdoc />
    public IPlatformSpecificBleCentral Central => throw new NotSupportedException();
    /// <inheritdoc />
    public IPlatformSpecificBlePeripheral? Peripheral { get; }

    /// <inheritdoc />
#pragma warning disable CA1822
    public string Identifier => "Darp.Ble.WinRT";
#pragma warning restore CA1822

    public void Dispose() => Host.Dispose();
}