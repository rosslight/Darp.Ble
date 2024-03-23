using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble.WinRT;

public enum ScanningMode
{
    Passive = 0,
    Active = 1,
    None = 2
}

public enum ScanTiming : ushort
{
    Default = Ms100,
    Ms100 = 160,
    Ms1000 = 1600
}

public class WinBleObserver : IBleObserverImplementation
{
    private BluetoothLEAdvertisementWatcher? _watcher;

    [MemberNotNull(nameof(_watcher))]
    private void CreateScanners(ScanningMode mode)
    {
        _watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = mode switch
            {
                ScanningMode.Passive => BluetoothLEScanningMode.Passive,
                ScanningMode.Active => BluetoothLEScanningMode.Active,
                ScanningMode.None => BluetoothLEScanningMode.None,
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            }
        };
        // Subscription needed for the watcher to start (observable will only subscribe later)
        _watcher.Received += (_, _) => { };
        _watcher.Stopped += (_, _) => { };
    }

    public bool TryStartScan(BleObserver bleObserver, out IObservable<IGapAdvertisement> observable)
    {
        CreateScanners(ScanningMode.Active);
        try
        {
            _watcher.Start();
        }
        catch (Exception e)
        {
            observable = Observable.Throw<IGapAdvertisement>(e);
            return false;
        }
        if (_watcher.Status is BluetoothLEAdvertisementWatcherStatus.Aborted)
        {
            var exception = new Exception(
                $"Watcher status is '{_watcher.Status}' but should be 'Created'.\nTry restarting the bluetooth adapter!");
            observable = Observable.Throw<IGapAdvertisement>(exception);
            return false;
        }
        _watcher.Stopped += (_, args) =>
        {
            if (args.Error is BluetoothError.Success)
                return;
            var exception = new Exception($"Watcher stopped with error {args.Error}");
            bleObserver.StopScan();
        };
        observable = Observable.FromEventPattern<
                TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>,
                BluetoothLEAdvertisementWatcher,
                BluetoothLEAdvertisementReceivedEventArgs>(
                addHandler => _watcher.Received += addHandler,
                removeHandler => _watcher.Received -= removeHandler)
            .Select(adv => OnAdvertisementReport(bleObserver, adv));
        return true;
    }

    public void StopScan()
    {
        _watcher?.Stop();
        _watcher = null;
    }

    private static GapAdvertisement OnAdvertisementReport(BleObserver bleObserver,
        IEventPattern<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs> gapEvt)
    {
        BluetoothLEAdvertisementReceivedEventArgs eventArgs = gapEvt.EventArgs;

        BleEventType advertisementType = eventArgs.AdvertisementType switch
        {
            BluetoothLEAdvertisementType.ConnectableUndirected => BleEventType.AdvInd,
            BluetoothLEAdvertisementType.ConnectableDirected => BleEventType.AdvDirectInd,
            BluetoothLEAdvertisementType.ScannableUndirected => BleEventType.AdvScanInd,
            BluetoothLEAdvertisementType.NonConnectableUndirected => BleEventType.AdvNonConnInd,
            BluetoothLEAdvertisementType.ScanResponse => BleEventType.ScanResponse,
            _ => (BleEventType)eventArgs.AdvertisementType
        };
        BleAddressType addressType = eventArgs.BluetoothAddressType switch
        {
            BluetoothAddressType.Public => BleAddressType.Public,
            BluetoothAddressType.Random => BleAddressType.RandomPrivateNonResolvable,
            _ => BleAddressType.NotAvailable
        };

        (AdTypes, byte[])[] pduData = eventArgs
            .Advertisement
            .DataSections
            .Select(section => ((AdTypes)section.DataType, section.Data.ToArray()))
            .ToArray();

        GapAdvertisement advertisement = GapAdvertisement.FromExtendedAdvertisingReport(bleObserver,
            eventArgs.Timestamp,
            advertisementType,
            new BleAddress(addressType, (UInt48)eventArgs.BluetoothAddress),
            Physical.NotAvailable,
            Physical.NotAvailable,
            AdvertisingSId.NoAdIProvided,
            (TxPowerLevel?)eventArgs.TransmitPowerLevelInDBm ?? TxPowerLevel.NotAvailable,
            (Rssi)eventArgs.RawSignalStrengthInDBm,
            PeriodicAdvertisingInterval.NoPeriodicAdvertising,
            new BleAddress(BleAddressType.NotAvailable, (UInt48)0x000000000000),
            pduData);

        return advertisement;
    }
}