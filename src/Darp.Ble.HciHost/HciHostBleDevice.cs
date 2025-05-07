using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gatt.Services;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Hci.Transport;
using Darp.Ble.Implementation;
using UInt48 = Darp.Ble.Data.UInt48;

namespace Darp.Ble.HciHost;

/// <summary> Provides windows specific implementation of a ble device </summary>
internal sealed class HciHostBleDevice(
    string? name,
    BleAddress? randomAddress,
    ITransportLayer transportLayer,
    IServiceProvider serviceProvider
) : BleDevice(serviceProvider, serviceProvider.GetLogger<HciHostBleDevice>())
{
    public Hci.HciHost Host { get; } =
        new(
            transportLayer,
            randomAddress ?? BleAddress.NewRandomStaticAddress().Value,
            logger: serviceProvider.GetLogger<Hci.HciHost>()
        );

    public override string? Name { get; set; } = name;
    public override AppearanceValues Appearance => AppearanceValues.Unknown;

    public override BleAddress RandomAddress => new(BleAddressType.RandomStatic, (UInt48)Host.Address);

    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        await Host.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await SetRandomAddressAsync(RandomAddress, cancellationToken).ConfigureAwait(false);

        Observer = new HciHostBleObserver(this, ServiceProvider.GetLogger<HciHostBleObserver>());
        Central = new HciHostBleCentral(this, ServiceProvider.GetLogger<HciHostBleCentral>());

        HciLeReadMaximumAdvertisingDataLengthResult result = await Host.QueryCommandCompletionAsync<
            HciLeReadMaximumAdvertisingDataLengthCommand,
            HciLeReadMaximumAdvertisingDataLengthResult
        >(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        Broadcaster = new HciHostBleBroadcaster(
            this,
            result.MaxAdvertisingDataLength,
            ServiceProvider.GetLogger<HciHostBleBroadcaster>()
        );
        Peripheral = new HciHostBlePeripheral(this, ServiceProvider.GetLogger<HciHostBlePeripheral>());
        Peripheral.AddGapService();
        return InitializeResult.Success;
    }

    protected override async Task SetRandomAddressAsyncCore(
        BleAddress randomAddress,
        CancellationToken cancellationToken
    )
    {
        await Host.SetRandomAddressAsync(randomAddress.Value, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
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
