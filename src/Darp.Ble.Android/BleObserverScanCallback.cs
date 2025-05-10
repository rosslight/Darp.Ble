using Android.Bluetooth.LE;
using Android.Runtime;
using Android.Util;

namespace Darp.Ble.Android;

public sealed class BleObserverScanCallback(Action<ScanResult> onNext, Action<ScanFailure> onError) : ScanCallback
{
    private readonly Action<ScanResult> _onNext = onNext;
    private readonly Action<ScanFailure> _onError = onError;

    public BleObserverScanCallback(IntPtr _, JniHandleOwnership __)
        : this(null!, _ => { })
    {
        Log.Warn("adv", "Suspicious call to native constructor");
    }

    public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
    {
        base.OnScanResult(callbackType, result);
        if (result is null)
            return;
        _onNext(result);
    }

    public override void OnBatchScanResults(IList<ScanResult>? results)
    {
        base.OnBatchScanResults(results);
        if (results is null)
            return;
        foreach (ScanResult result in results)
        {
            _onNext(result);
        }
    }

    public override void OnScanFailed(ScanFailure errorCode)
    {
        base.OnScanFailed(errorCode);
        _onError(errorCode);
    }
}
