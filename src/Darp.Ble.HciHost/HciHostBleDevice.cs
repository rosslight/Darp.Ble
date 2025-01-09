using Darp.Ble.Data;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Hci.Transport;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

/// <summary> Provides windows specific implementation of a ble device </summary>
internal sealed class HciHostBleDevice(string port,
    string name,
    BleAddress? randomAddress,
    ILogger? logger) : BleDevice(logger)
{
    public Hci.HciHost Host { get; } = new(new H4TransportLayer(port, logger: logger), logger: logger);

    public override string Name { get; } = name;

    public BleAddress RandomAddress { get; private set; } = randomAddress ?? BleAddress.NewRandomStaticAddress();

    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        await Host.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await SetRandomAddressAsync(RandomAddress, cancellationToken).ConfigureAwait(false);

        Observer = new HciHostBleObserver(this, Logger);
        Central = new HciHostBleCentral(this, Logger);

        HciLeReadMaximumAdvertisingDataLengthResult result = await Host
            .QueryCommandCompletionAsync<HciLeReadMaximumAdvertisingDataLengthCommand, HciLeReadMaximumAdvertisingDataLengthResult>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        Broadcaster = new HciHostBleBroadcaster(this, result.MaxAdvertisingDataLength, Logger);
        return InitializeResult.Success;
    }

    protected override async Task SetRandomAddressAsyncCore(BleAddress randomAddress, CancellationToken cancellationToken)
    {
        var addressValue = randomAddress.Value.ToUInt64();
        await Host.QueryCommandCompletionAsync<HciLeSetRandomAddressCommand, HciLeSetRandomAddressResult>(
            new HciLeSetRandomAddressCommand(addressValue),
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
        RandomAddress = randomAddress;
    }

    /// <inheritdoc />
    public override string Identifier => "Darp.Ble.HciHost";

    /// <inheritdoc />
    protected override void DisposeCore()
    {
        Host.Dispose();
        base.DisposeCore();
    }
}