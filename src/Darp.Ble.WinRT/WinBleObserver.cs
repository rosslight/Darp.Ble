using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;

namespace Darp.Ble.WinRT;

/// <inheritdoc />
internal sealed class WinBleObserver(BleDevice device, ILogger<WinBleObserver> logger) : BleObserver(device, logger)
{
    private BluetoothLEAdvertisementWatcher? _watcher;
    private IDisposable? _observableSubscription;

    [MemberNotNull(nameof(_watcher))]
    private void CreateScanners(ScanType mode)
    {
        _watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = mode switch
            {
                ScanType.Passive => BluetoothLEScanningMode.Passive,
                ScanType.Active => BluetoothLEScanningMode.Active,
                _ => throw new ArgumentOutOfRangeException(nameof(mode)),
            },
            AllowExtendedAdvertisements = true,
        };
        // Subscription needed for the watcher to start (observable will only subscribe later)
        _watcher.Received += (_, _) => { };
        _watcher.Stopped += (_, _) => { };
    }

    protected override Task StartObservingAsyncCore(CancellationToken cancellationToken)
    {
        CreateScanners(Parameters.ScanType);
        _watcher.Start();
        if (_watcher.Status is BluetoothLEAdvertisementWatcherStatus.Aborted)
        {
            throw new BleObservationStartException(
                this,
                $"Watcher status is '{_watcher.Status}' but should be 'Created'.\nTry restarting the bluetooth adapter!"
            );
        }
        _watcher.Stopped += async (_, args) =>
        {
            if (args.Error is BluetoothError.Success)
                return;
            Logger.LogError("Watcher stopped with error {Error}", args.Error);
            await StopObservingAsync().ConfigureAwait(false);
        };
        _observableSubscription = Observable
            .FromEventPattern<
                TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>,
                BluetoothLEAdvertisementWatcher,
                BluetoothLEAdvertisementReceivedEventArgs
            >(addHandler => _watcher.Received += addHandler, removeHandler => _watcher.Received -= removeHandler)
            .Select(adv => OnAdvertisementReport(this, adv))
            .Subscribe(OnNext);
        return Task.CompletedTask;
    }

    protected override Task StopObservingAsyncCore()
    {
        _observableSubscription?.Dispose();
        _watcher?.Stop();
        _watcher = null;
        return Task.CompletedTask;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _observableSubscription?.Dispose();
        _watcher?.Stop();
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    private static GapAdvertisement OnAdvertisementReport(
        BleObserver bleObserver,
        EventPattern<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs> gapEvt
    )
    {
        BluetoothLEAdvertisementReceivedEventArgs eventArgs = gapEvt.EventArgs;

        BleEventType advertisementType = eventArgs.AdvertisementType switch
        {
            BluetoothLEAdvertisementType.ConnectableUndirected => BleEventType.AdvInd,
            BluetoothLEAdvertisementType.ConnectableDirected => BleEventType.AdvDirectInd,
            BluetoothLEAdvertisementType.ScannableUndirected => BleEventType.AdvScanInd,
            BluetoothLEAdvertisementType.NonConnectableUndirected => BleEventType.AdvNonConnInd,
            BluetoothLEAdvertisementType.ScanResponse => BleEventType.ScanResponse,
            _ => (BleEventType)eventArgs.AdvertisementType,
        };

        (AdTypes, ReadOnlyMemory<byte>)[] pduData = eventArgs
            .Advertisement.DataSections.Select(section =>
                ((AdTypes)section.DataType, (ReadOnlyMemory<byte>)section.Data.ToArray())
            )
            .ToArray();

        GapAdvertisement advertisement = GapAdvertisement.FromExtendedAdvertisingReport(
            bleObserver,
            eventArgs.Timestamp,
            advertisementType,
            BleHelper.GetBleAddress(eventArgs.BluetoothAddress, eventArgs.BluetoothAddressType),
            Physical.NotAvailable,
            Physical.NotAvailable,
            AdvertisingSId.NoAdIProvided,
            (TxPowerLevel?)eventArgs.TransmitPowerLevelInDBm ?? TxPowerLevel.NotAvailable,
            (Rssi)eventArgs.RawSignalStrengthInDBm,
            PeriodicAdvertisingInterval.NoPeriodicAdvertising,
            new BleAddress(BleAddressType.NotAvailable, (UInt48)0x000000000000),
            AdvertisingData.From(pduData)
        );

        return advertisement;
    }
}
