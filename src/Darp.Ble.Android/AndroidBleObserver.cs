using System.Reactive.Linq;
using Android.Bluetooth.LE;
using Android.Content;
using Android.Locations;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Android;

public sealed class AndroidBleObserver(
    BleDevice device,
    BluetoothLeScanner bluetoothLeScanner,
    ILogger<AndroidBleObserver> logger
) : BleObserver(device, logger)
{
    private readonly BluetoothLeScanner _bluetoothLeScanner = bluetoothLeScanner;
    private BleObserverScanCallback? _scanCallback;

    protected override bool TryStartScanCore(out IObservable<IGapAdvertisement> observable)
    {
        // Android versions before Android12 (API version <= 30) did have to Location services at all times,
        // when targeting >= 31, there is an option to assert that there is no usage of location in the manifest
        // https://developer.android.com/develop/connectivity/bluetooth/bt-permissions#declare-android11-or-lower
        // TODO Best case: We do not throw and just warn the user about possibly inappropriate usage. How to do that in a cross-platform way?
        if (!AreLocationServicesEnabled())
        {
            observable = Observable.Throw<IGapAdvertisement>(
                new BleObservationStartException(
                    this,
                    "Location services are not enabled. Please check in the settings"
                )
            );
            return false;
        }
        _scanCallback = new BleObserverScanCallback(this);
        using var settingsBuilder = new ScanSettings.Builder();
        ScanSettings? scanSettings = settingsBuilder
            .SetCallbackType(ScanCallbackType.AllMatches)
            ?.SetMatchMode(BluetoothScanMatchMode.Aggressive)
            ?.SetReportDelay(0)
            ?.Build();
        _bluetoothLeScanner.StartScan(filters: null, scanSettings, _scanCallback);
        observable = _scanCallback.Select(x => OnAdvertisementReport(this, x));
        return true;
    }

    protected override void StopScanCore()
    {
        if (_scanCallback is null)
            return;
        _bluetoothLeScanner.StopScan(_scanCallback);
        _bluetoothLeScanner.FlushPendingScanResults(_scanCallback);
        _scanCallback.Dispose();
        _scanCallback = null;
    }

    private static GapAdvertisement OnAdvertisementReport(
        BleObserver bleObserver,
        ScanResult scanResult
    )
    {
        // Extract the very little information about the event type we have left
        var advertisementType = BleEventType.None;
        if (scanResult.IsLegacy)
            advertisementType |= BleEventType.Legacy;
        if (scanResult.IsConnectable)
            advertisementType |= BleEventType.Connectable;

        // Assume address string is hex
        string? addressString = scanResult.Device?.Address;
        BleAddress address = addressString is not null
            ? BleAddress.Parse(addressString, provider: null)
            : new BleAddress(BleAddressType.NotAvailable, (UInt48)0x00);

        AdvertisingData advertisingData = AdvertisingData.From(scanResult.ScanRecord?.GetBytes());

        GapAdvertisement advertisement = GapAdvertisement.FromExtendedAdvertisingReport(
            bleObserver,
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
            advertisingData
        );

        return advertisement;
    }

    private static bool AreLocationServicesEnabled()
    {
        if (
            Application.Context.GetSystemService(Context.LocationService)
            is not LocationManager locationManager
        )
            return false;
        try
        {
            bool isGpsEnabled = locationManager.IsProviderEnabled(LocationManager.GpsProvider);
            bool isNetworkEnabled = locationManager.IsProviderEnabled(LocationManager.GpsProvider);
            return isGpsEnabled && isNetworkEnabled;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
