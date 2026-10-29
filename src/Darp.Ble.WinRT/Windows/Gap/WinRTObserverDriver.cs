using System.Runtime.InteropServices.WindowsRuntime;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;

namespace Darp.Ble.WinRT.Gap;

/// <summary>
/// One watcher per radio scan. Windows chooses scan interval and window itself; only the scan type applies.
/// Each scan's callbacks belong to its own watcher, so a stopped scan never reports into the next one.
/// When the radio turns off, Windows stops the watcher for good, so the scan fails
/// </summary>
internal sealed class WinRTObserverDriver(Radio radio, TimeProvider timeProvider) : IObserverDriver, IAsyncDisposable
{
    private readonly Radio _radio = radio;
    private readonly TimeProvider _timeProvider = timeProvider;
    private Run? _run;

    public Task StartScanAsync(BleScanParameters parameters, IScanSink sink, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WinRTRadio.ThrowIfOff(_radio);
        var watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode =
                parameters.ScanType is ScanType.Active
                    ? BluetoothLEScanningMode.Active
                    : BluetoothLEScanningMode.Passive,
            AllowExtendedAdvertisements = true,
        };
        var run = new Run(watcher, sink, _timeProvider);
        _run = run;
        // Both handlers before Start, so no report or stop of this run is missed
        watcher.Received += run.OnReceived;
        watcher.Stopped += run.OnStopped;
        watcher.Start();
        if (watcher.Status is BluetoothLEAdvertisementWatcherStatus.Aborted)
        {
            run.Detach();
            _run = null;
            throw new BleException("Windows could not start scanning; the Bluetooth adapter may need a restart");
        }
        return Task.CompletedTask;
    }

    public async Task StopScanAsync(CancellationToken cancellationToken)
    {
        Run? run = _run;
        _run = null;
        if (run is not null)
            await run.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => new(StopScanAsync(CancellationToken.None));

    /// <summary> One radio scan with its watcher and the sink it reports to </summary>
    private sealed class Run(BluetoothLEAdvertisementWatcher watcher, IScanSink sink, TimeProvider timeProvider)
    {
        private readonly TaskCompletionSource _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private volatile bool _detached;

        public void OnReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs report)
        {
            if (_detached)
                return;
            sink.OnReport(ToAdvertisement(report, timeProvider.GetUtcNow()));
        }

        public void OnStopped(
            BluetoothLEAdvertisementWatcher sender,
            BluetoothLEAdvertisementWatcherStoppedEventArgs args
        )
        {
            _stopped.TrySetResult();
            // A stop this driver asked for detached the run first; any other stop ends the scan
            if (_detached)
                return;
            _detached = true;
            sink.OnFailed(
                args.Error is BluetoothError.Success
                    // Windows reports no error when the radio turns off
                    ? new BleException(
                        "Windows stopped scanning; the Bluetooth radio may be off",
                        isTransient: true,
                        nativeCode: null,
                        innerException: null
                    )
                    : new BleException(
                        $"Windows stopped scanning: {args.Error}",
                        isTransient: args.Error is BluetoothError.RadioNotAvailable,
                        nativeCode: null,
                        innerException: null
                    )
            );
        }

        /// <summary> Stop only asks the watcher to stop; it has stopped once it reports so </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _detached = true;
            if (
                watcher.Status
                is BluetoothLEAdvertisementWatcherStatus.Started
                    or BluetoothLEAdvertisementWatcherStatus.Stopping
            )
            {
                watcher.Stop();
                await _stopped.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            Detach();
        }

        public void Detach()
        {
            _detached = true;
            watcher.Received -= OnReceived;
            watcher.Stopped -= OnStopped;
        }
    }

    private static GapAdvertisement ToAdvertisement(
        BluetoothLEAdvertisementReceivedEventArgs report,
        DateTimeOffset timestamp
    )
    {
        (AdTypes Section, ReadOnlyMemory<byte> Bytes)[] sections =
        [
            .. report.Advertisement.DataSections.Select(section =>
                ((AdTypes)section.DataType, (ReadOnlyMemory<byte>)section.Data.ToArray())
            ),
        ];
        return GapAdvertisement.FromExtendedAdvertisingReport(
            timestamp,
            WinRTMapping.ToEventType(report),
            WinRTMapping.ToAddress(report.BluetoothAddress, report.BluetoothAddressType),
            Physical.NotAvailable,
            Physical.NotAvailable,
            AdvertisingSId.NoAdIProvided,
            report.TransmitPowerLevelInDBm is { } power ? (TxPowerLevel)power : TxPowerLevel.NotAvailable,
            (Rssi)report.RawSignalStrengthInDBm,
            PeriodicAdvertisingInterval.NoPeriodicAdvertising,
            BleAddress.NotAvailable,
            AdvertisingData.From(sections)
        );
    }
}
