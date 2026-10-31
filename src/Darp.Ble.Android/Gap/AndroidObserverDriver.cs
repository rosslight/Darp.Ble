using Android;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Android.Gap;

/// <summary>
/// One scan callback per radio scan, so a stopped scan never reports into the next one. Android chooses the scan
/// type, interval and window itself; the driver asks for its fastest scan mode. It reports legacy advertisements only,
/// as Android's default scan settings do. Android ends a scan without a callback
/// when Bluetooth is turned off, so each run watches the adapter's state
/// </summary>
internal sealed class AndroidObserverDriver(BluetoothAdapter adapter, TimeProvider timeProvider) : IObserverDriver
{
    private readonly BluetoothAdapter _adapter = adapter;
    private readonly TimeProvider _timeProvider = timeProvider;
    private Run? _run;

    public Task StartScanAsync(BleScanParameters parameters, IScanSink sink, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BluetoothLeScanner scanner =
            _adapter.BluetoothLeScanner
            ?? throw new BleException(
                "Bluetooth is turned off",
                isTransient: true,
                nativeCode: null,
                innerException: null
            );
        ThrowIfLocationIsOff();
        using var builder = new ScanSettings.Builder();
        builder.SetScanMode(global::Android.Bluetooth.LE.ScanMode.LowLatency);
        using ScanSettings settings =
            builder.Build() ?? throw new BleException("Android could not create the scan settings");
        Run? run = new Run(scanner, sink, _timeProvider);
        try
        {
            run.WatchBluetoothState();
            scanner.StartScan(filters: null, settings, run);
            _run = run;
            run = null;
        }
        catch (Java.Lang.Exception e)
        {
            throw new BleException($"Android could not start scanning: {e.Message}", e);
        }
        finally
        {
            // Not started: nothing of it stays
            run?.Stop();
            run?.Dispose();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// An app that may derive location from scans gets no results while location services are off, and Android tells it
    /// nothing; an app that declared neverForLocation on BLUETOOTH_SCAN scans regardless
    /// </summary>
    private static void ThrowIfLocationIsOff()
    {
        Context app = Application.Context;
        if (app.GetSystemService(Context.LocationService) is not LocationManager { IsLocationEnabled: false })
            return;
        if (DeclaresNeverForLocation(app))
            return;
        throw new BleException(
            "Location services are off, so Android delivers no scan results; turn them on, or declare neverForLocation on BLUETOOTH_SCAN",
            isTransient: true,
            nativeCode: null,
            innerException: null
        );
    }

    private static bool DeclaresNeverForLocation(Context app)
    {
        string name = app.PackageName ?? "";
        PackageInfo? info = OperatingSystem.IsAndroidVersionAtLeast(33)
            ? app.PackageManager?.GetPackageInfo(
                name,
                PackageManager.PackageInfoFlags.Of((long)PackageInfoFlags.Permissions)
            )
            : app.PackageManager?.GetPackageInfo(name, PackageInfoFlags.Permissions);
        IList<string>? permissions = info?.RequestedPermissions;
        IList<int>? flags = info?.RequestedPermissionsFlags;
        if (permissions is null || flags is null)
            return false;
        for (var index = 0; index < permissions.Count && index < flags.Count; index++)
        {
            if (string.Equals(permissions[index], Manifest.Permission.BluetoothScan, StringComparison.Ordinal))
                return ((RequestedPermission)flags[index]).HasFlag(RequestedPermission.NeverForLocation);
        }
        return false;
    }

    /// <summary> Android stops at once </summary>
    public Task StopScanAsync(CancellationToken cancellationToken)
    {
        Run? run = _run;
        _run = null;
        run?.Stop();
        run?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary> One radio scan and the sink it reports to </summary>
    private sealed class Run(BluetoothLeScanner scanner, IScanSink sink, TimeProvider timeProvider) : ScanCallback
    {
#pragma warning disable CA2213 // The adapter owns the scanner
        private readonly BluetoothLeScanner _scanner = scanner;
#pragma warning restore CA2213
        private readonly IScanSink _sink = sink;
        private readonly TimeProvider _timeProvider = timeProvider;
        private readonly Lock _lock = new();
#pragma warning disable CA2213 // Disposed once the run stops watching
        private BluetoothStateReceiver? _stateReceiver;
#pragma warning restore CA2213
        private int _stopped;

        public void WatchBluetoothState()
        {
            var receiver = new BluetoothStateReceiver(this);
            lock (_lock)
                _stateReceiver = receiver;
            // A system broadcast, so it needs no export flag
            using var filter = new IntentFilter(BluetoothAdapter.ActionStateChanged);
            Application.Context.RegisterReceiver(receiver, filter);
        }

        public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
        {
            if (Volatile.Read(ref _stopped) is 1 || result is null)
                return;
            _sink.OnReport(ToAdvertisement(result, _timeProvider.GetUtcNow()));
        }

        // SCAN_FAILED_OUT_OF_HARDWARE_RESOURCES and SCAN_FAILED_SCANNING_TOO_FREQUENTLY; the binding names them from Android 13 on only
        private const ScanFailure OutOfHardwareResources = (ScanFailure)5;
        private const ScanFailure ScanningTooFrequently = (ScanFailure)6;

        // Scanning too often and the hardware being busy pass with time; the other failures do not
        public override void OnScanFailed(ScanFailure errorCode) =>
            Fail(
                new BleException(
                    $"Android stopped scanning: {errorCode}",
                    isTransient: errorCode is ScanningTooFrequently or OutOfHardwareResources,
                    (int)errorCode,
                    null
                )
            );

        public void OnBluetoothOff() =>
            Fail(
                new BleException("Bluetooth was turned off", isTransient: true, nativeCode: null, innerException: null)
            );

        /// <summary> Reports the first failure only, and none after a stop </summary>
        private void Fail(BleException error)
        {
            if (Interlocked.Exchange(ref _stopped, 1) is 1)
                return;
            StopWatching();
            _sink.OnFailed(error);
        }

        public void Stop()
        {
            Volatile.Write(ref _stopped, 1);
            StopWatching();
            try
            {
                _scanner.StopScan(this);
            }
            catch (Java.Lang.IllegalStateException)
            {
                // Bluetooth was turned off, which ended the scan already
            }
        }

        private void StopWatching()
        {
            BluetoothStateReceiver? receiver;
            lock (_lock)
            {
                receiver = _stateReceiver;
                _stateReceiver = null;
            }
            if (receiver is null)
                return;
            Application.Context.UnregisterReceiver(receiver);
            receiver.Dispose();
        }
    }

    /// <summary> Tells its run when Bluetooth turns off </summary>
    private sealed class BluetoothStateReceiver(Run run) : BroadcastReceiver
    {
#pragma warning disable CA2213 // The driver owns the run
        private readonly Run _run = run;
#pragma warning restore CA2213

        public override void OnReceive(Context? context, Intent? intent)
        {
            int state = intent?.GetIntExtra(BluetoothAdapter.ExtraState, -1) ?? -1;
            if (state is (int)State.TurningOff or (int)State.Off)
                _run.OnBluetoothOff();
        }
    }

    private static GapAdvertisement ToAdvertisement(ScanResult result, DateTimeOffset timestamp)
    {
        BleEventType type =
            (result.IsLegacy ? BleEventType.Legacy : BleEventType.None)
            | (result.IsConnectable ? BleEventType.Connectable : BleEventType.None);
        BleAddress address = BleAddress.TryParse(result.Device?.Address, provider: null, out BleAddress? parsed)
            ? parsed
            : BleAddress.NotAvailable;
        return GapAdvertisement.Create(
            timestamp,
            type,
            address,
            (Physical)result.PrimaryPhy,
            (Physical)result.SecondaryPhy,
            (AdvertisingSId)result.AdvertisingSid,
            (TxPowerLevel)result.TxPower,
            (Rssi)result.Rssi,
            (PeriodicAdvertisingInterval)result.PeriodicAdvertisingInterval,
            BleAddress.NotAvailable,
            AdvertisingData.From(result.ScanRecord?.GetBytes() ?? [])
        );
    }
}
