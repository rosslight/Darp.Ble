using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gatt.Services;
using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Hci.Transport;
using Darp.Ble.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    public Hci.HciDevice HciDevice { get; } =
        new(
            transportLayer,
            randomAddress ?? BleAddress.NewRandomStaticAddress().Value,
            serviceProvider.GetService<ILoggerFactory>()
        );

    public override string? Name { get; set; } = name;
    public override AppearanceValues Appearance { get; set; } = AppearanceValues.Unknown;

    public override BleAddress RandomAddress => BleAddress.CreateRandomAddress((UInt48)HciDevice.Address);

    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        await HciDevice.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await SetRandomAddressAsync(RandomAddress, cancellationToken).ConfigureAwait(false);

        Observer = new HciHostBleObserver(this, ServiceProvider.GetLogger<HciHostBleObserver>());
        Central = new HciHostBleCentral(this, ServiceProvider.GetLogger<HciHostBleCentral>());

        HciLeReadMaximumAdvertisingDataLengthResult result = await HciDevice
            .Host.QueryCommandCompletionAsync<
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
        await HciDevice
            .SetRandomAddressAsync(randomAddress.Value, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override string Identifier => BleDeviceIdentifiers.HciHost;

    protected override async ValueTask DisposeAsyncCore()
    {
        await base.DisposeAsyncCore().ConfigureAwait(false);
        await HciDevice.DisposeAsync().ConfigureAwait(false);
    }
}
