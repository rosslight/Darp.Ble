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
        };
        // Subscription needed for the watcher to start (observable will only subscribe later)
        _watcher.Received += (_, _) => { };
        _watcher.Stopped += (_, _) => { };
    }

    /// <inheritdoc />
    protected override bool TryStartScanCore(out IObservable<IGapAdvertisement> observable)
    {
        CreateScanners(ScanType.Active);
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
            var exception = new BleObservationStartException(
                this,
                $"Watcher status is '{_watcher.Status}' but should be 'Created'.\nTry restarting the bluetooth adapter!"
            );
            observable = Observable.Throw<IGapAdvertisement>(exception);
            return false;
        }
        _watcher.Stopped += (_, args) =>
        {
            if (args.Error is BluetoothError.Success)
                return;
            var exception = new BleObservationStopException(this, $"Watcher stopped with error {args.Error}");
            StopScan(exception);
        };
        observable = Observable
            .FromEventPattern<
                TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>,
                BluetoothLEAdvertisementWatcher,
                BluetoothLEAdvertisementReceivedEventArgs
            >(addHandler => _watcher.Received += addHandler, removeHandler => _watcher.Received -= removeHandler)
            .Select(adv => OnAdvertisementReport(this, adv));
        return true;
    }

    /// <inheritdoc />
    protected override void StopScanCore()
    {
        _watcher?.Stop();
        _watcher = null;
    }

    private static GapAdvertisement OnAdvertisementReport(
        BleObserver bleObserver,
        IEventPattern<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs> gapEvt
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
