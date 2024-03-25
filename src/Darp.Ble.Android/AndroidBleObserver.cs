using System.Reactive.Linq;
using Android.Bluetooth.LE;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble.Android;

public sealed class AndroidBleObserver(BluetoothLeScanner bluetoothLeScanner) : IPlatformSpecificBleObserver, IDisposable
{
    private readonly BluetoothLeScanner _bluetoothLeScanner = bluetoothLeScanner;
    private BleObserverScanCallback? _scanCallback;

    public bool TryStartScan(BleObserver observer, out IObservable<IGapAdvertisement> observable)
    {
        _scanCallback = new BleObserverScanCallback(observer);
        ScanSettings? scanSettings = new ScanSettings.Builder()
            .SetCallbackType(ScanCallbackType.AllMatches)
            ?.SetMatchMode(BluetoothScanMatchMode.Aggressive)
            ?.SetReportDelay(0)
            ?.Build();
        _bluetoothLeScanner.StartScan(filters: null, scanSettings, _scanCallback);
        observable = _scanCallback
            .Select(x => OnAdvertisementReport(observer, x));
        return true;
    }

    public void StopScan()
    {
        _bluetoothLeScanner.StopScan((ScanCallback?)null);
        _scanCallback?.Dispose();
        _scanCallback = null;
    }

    private static GapAdvertisement OnAdvertisementReport(BleObserver bleObserver, ScanResult scanResult)
    {
        // Extract the very little information about the event type we have left
        var advertisementType = BleEventType.None;
        if (scanResult.IsLegacy) advertisementType |= BleEventType.Legacy;
        if (scanResult.IsConnectable) advertisementType |= BleEventType.Connectable;

        // Assume address string is hex
        string? addressString = scanResult.Device?.Address;
        BleAddress address = addressString is not null
            ? BleAddress.Parse(addressString, provider: null)
            : new BleAddress(BleAddressType.NotAvailable, (UInt48)0x00);

        AdvertisingData advertisingData = AdvertisingData.From(scanResult.ScanRecord?.GetBytes());

        GapAdvertisement advertisement = GapAdvertisement.FromExtendedAdvertisingReport(bleObserver,
            DateTimeOffset.UtcNow,
            advertisementType,
            address,
            (Physical)scanResult.PrimaryPhy,
            (Physical)scanResult.SecondaryPhy,
            (AdvertisingSId)scanResult.AdvertisingSid,
            (TxPowerLevel)scanResult.TxPower,
            (Rssi)scanResult.Rssi,
            (PeriodicAdvertisingInterval)scanResult.PeriodicAdvertisingInterval,
            new BleAddress(BleAddressType.NotAvailable, (UInt48)0x000000000000),
            advertisingData);

        return advertisement;
    }

    void IDisposable.Dispose() => StopScan();
}