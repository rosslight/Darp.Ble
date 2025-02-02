using Darp.Ble.Data;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Hci.Transport;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

/// <summary> Provides windows specific implementation of a ble device </summary>
internal sealed class HciHostBleDevice(
    string port,
    string name,
    BleAddress? randomAddress,
    ILoggerFactory loggerFactory
) : BleDevice(loggerFactory, loggerFactory.CreateLogger<HciHostBleDevice>())
{
    public Hci.HciHost Host { get; } =
        new(
            new H4TransportLayer(port, logger: loggerFactory.CreateLogger<H4TransportLayer>()),
            logger: loggerFactory.CreateLogger<Hci.HciHost>()
        );

    public override string Name { get; } = name;

    public BleAddress RandomAddress { get; private set; } = randomAddress ?? BleAddress.NewRandomStaticAddress();

    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        await Host.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await SetRandomAddressAsync(RandomAddress, cancellationToken).ConfigureAwait(false);

        Observer = new HciHostBleObserver(this, LoggerFactory.CreateLogger<HciHostBleObserver>());
        Central = new HciHostBleCentral(this, LoggerFactory.CreateLogger<HciHostBleCentral>());

        HciLeReadMaximumAdvertisingDataLengthResult result = await Host.QueryCommandCompletionAsync<
            HciLeReadMaximumAdvertisingDataLengthCommand,
            HciLeReadMaximumAdvertisingDataLengthResult
        >(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        Broadcaster = new HciHostBleBroadcaster(
            this,
            result.MaxAdvertisingDataLength,
            LoggerFactory.CreateLogger<HciHostBleBroadcaster>()
        );
        return InitializeResult.Success;
    }

    protected override async Task SetRandomAddressAsyncCore(
        BleAddress randomAddress,
        CancellationToken cancellationToken
    )
    {
        var addressValue = randomAddress.Value.ToUInt64();
        await Host.QueryCommandCompletionAsync<HciLeSetRandomAddressCommand, HciLeSetRandomAddressResult>(
                new HciLeSetRandomAddressCommand(addressValue),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        RandomAddress = randomAddress;
    }

    /// <inheritdoc />
    public override string Identifier => BleDeviceIdentifiers.HciHost;

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        Host.Dispose();
        base.Dispose(disposing);
    }
}
