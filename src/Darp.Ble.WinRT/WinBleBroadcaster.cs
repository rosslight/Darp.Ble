using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.Utils;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT;


internal sealed class WinBleBroadcaster(WinBleDevice winBleDevice, ILogger? logger) : BleBroadcaster(logger)
{
    private readonly WinBleDevice _winBleDevice = winBleDevice;

    protected override Task<IAdvertisingSet> CreateAdvertisingSetAsyncCore(AdvertisingParameters parameters,
        AdvertisingData data,
        AdvertisingData? scanResponseData,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IAdvertisingSet>(new WinAdvertisingSet(this,
            BleAddress.NotAvailable,
            parameters,
            data,
            scanResponseData,
            TxPowerLevel.NotAvailable));
    }

    protected override Task<IAsyncDisposable> StartAdvertisingCoreAsync(
        IReadOnlyCollection<(IAdvertisingSet AdvertisingSet, TimeSpan Duration, byte NumberOfEvents)> advertisingSets,
        CancellationToken cancellationToken)
    {
        List<IAsyncDisposable> disposables = [];
        foreach ((IAdvertisingSet advertisingSet, TimeSpan duration, int numberOfEvents) in advertisingSets)
        {
            if (_winBleDevice.Capabilities.HasFlag(Capabilities.Peripheral))
            {
                var peripheral = (WinBlePeripheral)_winBleDevice.Peripheral;
                if (peripheral.Services.Count > 0)
                {
                    IAsyncDisposable d =  peripheral.AdvertiseServices(advertisingSet);
                    if (duration > TimeSpan.Zero)
                    {
                        var source = new CancellationTokenSource(duration);
                        source.Token.Register(async () => await d.DisposeAsync());
                    }
                    if (numberOfEvents > 0)
                    {
                        ArgumentOutOfRangeException.ThrowIfGreaterThan(numberOfEvents, 0);
                    }
                    disposables.Add(d);
                    continue;
                }
            }
            var publisher = new BluetoothLEAdvertisementPublisher();

            foreach ((AdTypes type, ReadOnlyMemory<byte> bytes) in advertisingSet.Data)
            {
                // Reserved types: https://learn.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.advertisement.bluetoothleadvertisementpublisher?view=winrt-22621
                if (type is AdTypes.Flags
                    or AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids
                    or AdTypes.CompleteListOf16BitServiceOrServiceClassUuids
                    or AdTypes.IncompleteListOf32BitServiceOrServiceClassUuids
                    or AdTypes.CompleteListOf32BitServiceOrServiceClassUuids
                    or AdTypes.IncompleteListOf128BitServiceOrServiceClassUuids
                    or AdTypes.CompleteListOf128BitServiceOrServiceClassUuids
                    or AdTypes.ShortenedLocalName
                    or AdTypes.CompleteLocalName
                    or AdTypes.TxPowerLevel
                    or AdTypes.ClassOfDevice
                    or AdTypes.SimplePairingHashC192
                    or AdTypes.SimplePairingRandomizerR192
                    or AdTypes.SecurityManagerTkValue
                    or AdTypes.SecurityManagerOutOfBandFlags
                    or AdTypes.PeripheralConnectionIntervalRange
                    or AdTypes.ListOf16BitServiceSolicitationUuids
                    or AdTypes.ListOf32BitServiceSolicitationUuids
                    or AdTypes.ListOf128BitServiceSolicitationUuids
                    or AdTypes.ServiceData16BitUuid
                    or AdTypes.ServiceData32BitUuid
                    or AdTypes.ServiceData128BitUuid
                    or AdTypes.PublicTargetAddress
                    or AdTypes.RandomTargetAddress
                    or AdTypes.Appearance
                    or AdTypes.AdvertisingInterval
                    or AdTypes.LeBluetoothDeviceAddress
                    or AdTypes.LeRole
                    or AdTypes.SimplePairingHashC256
                    or AdTypes.SimplePairingRandomizerR256
                    or AdTypes.ThreeDInformationData)
                {
                    Logger?.LogIgnoreDataSectionReservedType(type);
                    continue;
                }
                publisher.Advertisement.DataSections.Add(new BluetoothLEAdvertisementDataSection((byte)type, bytes.ToArray().AsBuffer()));
            }

            if (!advertisingSet.Parameters.Type.HasFlag(BleEventType.Legacy))
            {
                publisher.UseExtendedAdvertisement = true;
            }
            if (advertisingSet.Parameters.AdvertisingTxPower is not TxPowerLevel.NotAvailable)
            {
                publisher.IncludeTransmitPowerLevel = true;
                publisher.PreferredTransmitPowerLevelInDBm = (short)advertisingSet.Parameters.AdvertisingTxPower;
            }

            IAsyncDisposable disposable = AsyncDisposable.Create(publisher, state => state.Stop());
            // //publisher.UseExtendedAdvertisement = true;
            // //publisher.IncludeTransmitPowerLevel = true;
            publisher.StatusChanged += async (_, args) =>
            {
                if (args.Error is BluetoothError.Success) return;
                Logger?.LogPublisherChangedToError(args.Status, args.Error);
                await disposable.DisposeAsync().ConfigureAwait(false);
            };
            publisher.Start();
            disposables.Add(disposable);
        }
        IAsyncDisposable combinedDisposable = AsyncDisposable.Create(disposables, async x =>
        {
            foreach (IAsyncDisposable asyncDisposable in x)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
        });
        return Task.FromResult(combinedDisposable);
    }
}