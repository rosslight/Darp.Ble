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

    protected override Task StartObservingAsyncCore(CancellationToken cancellationToken)
    {
        // Android versions before Android12 (API version <= 30) did have to Location services at all times,
        // when targeting >= 31, there is an option to assert that there is no usage of location in the manifest
        // https://developer.android.com/develop/connectivity/bluetooth/bt-permissions#declare-android11-or-lower
        // TODO Best case: We do not throw and just warn the user about possibly inappropriate usage. How to do that in a cross-platform way?
        if (!AreLocationServicesEnabled())
        {
            throw new BleObservationStartException(
                this,
                "Location services are not enabled. Please check in the settings"
            );
        }
        _scanCallback = new BleObserverScanCallback(
            result =>
            {
                GapAdvertisement adv = OnAdvertisementReport(this, result);
                OnNext(adv);
            },
            failure =>
            {
                Logger.LogError("Scan failure because of {Failure}", failure);
                _ = StopObservingAsync();
            }
        );
        using var settingsBuilder = new ScanSettings.Builder();
        ScanSettings? scanSettings = settingsBuilder
            .SetCallbackType(ScanCallbackType.AllMatches)
            ?.SetMatchMode(BluetoothScanMatchMode.Aggressive)
            ?.SetReportDelay(0)
            ?.Build();
        _bluetoothLeScanner.StartScan(filters: null, scanSettings, _scanCallback);
        return Task.CompletedTask;
    }

    protected override Task StopObservingAsyncCore()
    {
        if (_scanCallback is null)
            return Task.CompletedTask;
        _bluetoothLeScanner.StopScan(_scanCallback);
        _bluetoothLeScanner.FlushPendingScanResults(_scanCallback);
        _scanCallback.Dispose();
        _scanCallback = null;
        return Task.CompletedTask;
    }

    private static GapAdvertisement OnAdvertisementReport(BleObserver bleObserver, ScanResult scanResult)
    {
        // Extract the very little information about the event type we have left
        var advertisementType = BleEventType.None;
        if (scanResult.IsLegacy)
            advertisementType |= BleEventType.Legacy;
        if (scanResult.IsConnectable)
            advertisementType |= BleEventType.Connectable;

        // Assume address string is hex
        string? addressString = scanResult.Device?.Address;
        BleAddress address = InternalHelpers.ParseBleAddress(addressString);

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
        if (Application.Context.GetSystemService(Context.LocationService) is not LocationManager locationManager)
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
