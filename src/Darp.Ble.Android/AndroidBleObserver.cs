using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.Bluetooth.LE;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;

namespace Darp.Ble.Android;

public class AndroidBleObserver : IBleObserverImplementation
{
    private readonly BluetoothLeScanner _bluetoothLeScanner;
    private MyScanCallback? _scanCallback;

    public AndroidBleObserver(BluetoothLeScanner bluetoothLeScanner)
    {
        _bluetoothLeScanner = bluetoothLeScanner;
    }

    public bool TryStartScan(BleObserver bleObserver, out IObservable<IGapAdvertisement> observable)
    {
        _scanCallback = new MyScanCallback();
        ScanSettings? scanSettings = new ScanSettings.Builder()
            .SetCallbackType(ScanCallbackType.AllMatches)
            ?.SetMatchMode(BluetoothScanMatchMode.Aggressive)
            ?.SetReportDelay(0)
            ?.Build();
        _bluetoothLeScanner.StartScan(null, scanSettings, _scanCallback);
        observable = _scanCallback
            .Select(x => OnAdvertisementReport(bleObserver, x));
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
        BleEventType advertisementType = scanResult.IsLegacy ? BleEventType.Legacy : 0;
        if (scanResult.IsConnectable) advertisementType |= BleEventType.Connectable;

        // Assume address string is hex
        var addressValue = Convert.ToUInt64(scanResult.Device?.Address?.Replace(":", ""), 16);

        AdvertisingData advertisingData = AdvertisingData.From(scanResult.ScanRecord?.GetBytes());

        GapAdvertisement advertisement = GapAdvertisement.FromExtendedAdvertisingReport(bleObserver,
            DateTimeOffset.UtcNow,
            advertisementType,
            new BleAddress(BleAddressType.NotAvailable, (UInt48)addressValue),
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
}

public class MyScanCallback : ScanCallback, IObservable<ScanResult>
{
    private readonly List<IObserver<ScanResult>> _observers = [];
    private bool _disposed;
    private readonly object _lockObject = new object();

    public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
    {
        base.OnScanResult(callbackType, result);
        if (result is null) return;
        foreach (IObserver<ScanResult> observer in _observers)
        {
            observer.OnNext(result);
        }
    }

    public override void OnBatchScanResults(IList<ScanResult>? results)
    {
        base.OnBatchScanResults(results);
        if (results is null) return;
        foreach (ScanResult scanResult in results)
        {
            foreach (IObserver<ScanResult> observer in _observers)
            {
                observer.OnNext(scanResult);
            }
        }
    }

    public override void OnScanFailed(ScanFailure scanFailure)
    {
        base.OnScanFailed(scanFailure);
        var scanFailedException = new Exception($"Scan failed because of {scanFailure}");
        foreach (IObserver<ScanResult> observer in _observers)
        {
            observer.OnError(scanFailedException);
        }
    }

    public IDisposable Subscribe(IObserver<ScanResult> observer)
    {
        lock (_lockObject)
        {
            if (_disposed) return Disposable.Empty;
            _observers.Add(observer);
            return Disposable.Create((ObserverList: _observers, Observer: observer), state =>
            {
                state.ObserverList.Remove(state.Observer);
            });
        }
    }

    protected override void Dispose(bool disposing)
    {
        lock (_lockObject)
        {
            base.Dispose(disposing);
            foreach (IObserver<ScanResult> observer in _observers)
            {
                observer.OnCompleted();
            }
            _disposed = true;
        }
    }
}