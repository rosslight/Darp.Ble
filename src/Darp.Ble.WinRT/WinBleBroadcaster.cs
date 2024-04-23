using System.Reactive.Disposables;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage.Streams;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Darp.Ble.Logger;

namespace Darp.Ble.WinRT;

internal sealed class WinBleBroadcaster(WinBleDevice winBleDevice, IObserver<LogEvent>? logger) : BleBroadcaster(logger)
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
                Logger?.Warning("Ignoring data section {Type}. This type is reserved by Windows", type);
                continue;
            }
            //publisher.IncludeTransmitPowerLevel = true;
            publisher.UseExtendedAdvertisement = true;
            publisher.Advertisement.DataSections.Add(new BluetoothLEAdvertisementDataSection((byte)type, bytes.ToArray().AsBuffer()));
        }
        // //publisher.UseExtendedAdvertisement = true;
        // //publisher.IncludeTransmitPowerLevel = true;
        publisher.StatusChanged += (sender, args) =>
        {
            int ii = 0;
        };
        publisher.Start();
        return Disposable.Create(publisher, state => state.Stop());
    }

    protected override void StopAllCore()
    {
        
    }
}