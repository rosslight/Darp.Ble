using System.Reactive.Disposables;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT;

internal sealed class WinBleBroadcaster(WinBleDevice winBleDevice, ILogger? logger) : BleBroadcaster(logger)
{
    private readonly WinBleDevice _winBleDevice = winBleDevice;

    protected override IDisposable AdvertiseCore(IObservable<AdvertisingData> source, AdvertisingParameters? parameters)
    {
        throw new NotImplementedException();
    }

    protected override IDisposable AdvertiseCore(AdvertisingData data, TimeSpan timeSpan, AdvertisingParameters? parameters)
    {
        var publisher = new BluetoothLEAdvertisementPublisher();

        foreach ((AdTypes type, ReadOnlyMemory<byte> bytes) in data)
        {
            // Reserved types: https://learn.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.advertisement.bluetoothleadvertisementpublisher?view=winrt-22621
            if (type is AdTypes.Flags
                or AdTypes.IncompleteListOf16BitServiceClassUuids
                or AdTypes.CompleteListOf16BitServiceClassUuids
                or AdTypes.IncompleteListOf32BitServiceClassUuids
                or AdTypes.CompleteListOf32BitServiceClassUuids
                or AdTypes.IncompleteListOf128BitServiceClassUuids
                or AdTypes.CompleteListOf128BitServiceClassUuids
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

        IDisposable disposable = Disposable.Create(publisher, state => state.Stop());
        // //publisher.UseExtendedAdvertisement = true;
        // //publisher.IncludeTransmitPowerLevel = true;
        publisher.StatusChanged += (_, args) =>
        {
            if (args.Error is BluetoothError.Success) return;
            Logger?.LogPublisherChangedToError(args.Status, args.Error);
            disposable.Dispose();
        };
        publisher.Start();
        return disposable;
    }

    protected override void StopAllCore()
    {
        
    }
}