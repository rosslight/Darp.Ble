using System.Reactive.Disposables;
using Android.Bluetooth.LE;
using Android.Runtime;
using Android.Util;
using Darp.Ble.Exceptions;
using Darp.Ble.Implementation;

namespace Darp.Ble.Android;

public sealed class BleObserverScanCallback(BleObserver bleObserver)
    : ScanCallback,
        IObservable<ScanResult>
{
    private readonly BleObserver _bleObserver = bleObserver;
    private readonly List<IObserver<ScanResult>> _observers = [];
    private bool _disposed;
    private readonly object _lockObject = new();

    public BleObserverScanCallback(IntPtr _, JniHandleOwnership __)
        : this(null!)
    {
        Log.Warn("adv", "Suspicious call to native constructor");
    }

    public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
    {
        base.OnScanResult(callbackType, result);
        if (result is null)
            return;
        foreach (IObserver<ScanResult> observer in _observers)
        {
            observer.OnNext(result);
        }
    }

    public override void OnBatchScanResults(IList<ScanResult>? results)
    {
        base.OnBatchScanResults(results);
        if (results is null)
            return;
        foreach (ScanResult scanResult in results)
        {
            foreach (IObserver<ScanResult> observer in _observers)
            {
                observer.OnNext(scanResult);
            }
        }
    }

    public override void OnScanFailed(ScanFailure errorCode)
    {
        base.OnScanFailed(errorCode);
        var scanFailedException = new BleObservationStopException(
            _bleObserver,
            $"Scan failed because of {errorCode}"
        );
        foreach (IObserver<ScanResult> observer in _observers)
        {
            observer.OnError(scanFailedException);
        }
    }

    public IDisposable Subscribe(IObserver<ScanResult> observer)
    {
        lock (_lockObject)
        {
            if (_disposed)
                return Disposable.Empty;
            _observers.Add(observer);
            return Disposable.Create(
                (ObserverList: _observers, Observer: observer),
                state =>
                {
                    state.ObserverList.Remove(state.Observer);
                }
            );
        }
    }

    protected override void Dispose(bool disposing)
    {
        lock (_lockObject)
        {
            base.Dispose(disposing);
            for (int index = _observers.Count - 1; index >= 0; index--)
            {
                IObserver<ScanResult> observer = _observers[index];
                observer.OnCompleted();
            }

            _disposed = true;
        }
    }
}
