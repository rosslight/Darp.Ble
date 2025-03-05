using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Implementation;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;
using UInt48 = Darp.Ble.Data.UInt48;

namespace Darp.Ble.HciHost;

/// <inheritdoc />
internal sealed class HciHostBleObserver(HciHostBleDevice device, ILogger<HciHostBleObserver> logger)
    : BleObserver(device, logger)
{
    private readonly HciHostBleDevice _device = device;

    private static (HciLeSetExtendedScanParametersCommand, HciLeSetExtendedScanEnableCommand) CreateConfiguration(
        BleScanParameters parameters
    )
    {
        bool isInActiveMode = parameters.ScanType is ScanType.Active;
        return (
            new HciLeSetExtendedScanParametersCommand
            {
                OwnAddressType = 0x01,
                ScanningFilterPolicy = 0x00,
                ScanPhys = 0b00000001,
                ScanType = isInActiveMode ? (byte)0x01 : (byte)0x00,
                ScanInterval = (ushort)parameters.ScanInterval,
                ScanWindow = (ushort)parameters.ScanWindow,
            },
            new HciLeSetExtendedScanEnableCommand
            {
                Enable = 0x01,
                FilterDuplicates = 0x00,
                Duration = 0x0000,
                Period = 0x0000,
            }
        );
    }

    /// <inheritdoc />
    protected override bool TryStartScanCore(out IObservable<IGapAdvertisement> observable)
    {
        (HciLeSetExtendedScanParametersCommand Parameters, HciLeSetExtendedScanEnableCommand Enable) commands =
            CreateConfiguration(Parameters);
        //Logger.Verbose("AdvertisingScanner: Using scan params {@ScanParams} and enable params {@EnableParams}", commands.Parameters, commands.Enable);
        observable = Observable.Create<IGapAdvertisement>(
            async (observer, token) =>
            {
                HciSetExtendedScanParametersResult paramSetResult = await _device
                    .Host.QueryCommandCompletionAsync<
                        HciLeSetExtendedScanParametersCommand,
                        HciSetExtendedScanParametersResult
                    >(commands.Parameters, cancellationToken: token)
                    .ConfigureAwait(false);
                if (paramSetResult.Status is not HciCommandStatus.Success)
                {
                    observer.OnError(
                        new BleObservationStartException(
                            this,
                            $"Could not set scan parameters: {paramSetResult.Status}"
                        )
                    );
                    return Disposable.Empty;
                }
                HciSetExtendedScanEnableResult enableResult = await _device
                    .Host.QueryCommandCompletionAsync<
                        HciLeSetExtendedScanEnableCommand,
                        HciSetExtendedScanEnableResult
                    >(commands.Enable, cancellationToken: token)
                    .ConfigureAwait(false);
                if (enableResult.Status is not HciCommandStatus.Success)
                {
                    observer.OnError(
                        new BleObservationStartException(this, $"Could not enable scan: {enableResult.Status}")
                    );
                    return Disposable.Empty;
                }

                return _device
                    .Host.AsObservable<HciLeExtendedAdvertisingReportEvent>()
                    .SelectMany(x => x.Reports)
                    .Select(x => OnAdvertisementReport(this, x))
                    .AsObservable()
                    .Subscribe(observer);
            }
        );
        return true;
    }

    /// <inheritdoc />
    protected override void StopScanCore()
    {
        var stopScanCommand = new HciLeSetExtendedScanEnableCommand
        {
            Enable = 0x00,
            FilterDuplicates = 0x00,
            Duration = 0x0000,
            Period = 0x0000,
        };
        _ = _device.Host.QueryCommandCompletionAsync<HciLeSetExtendedScanEnableCommand, HciSetExtendedScanEnableResult>(
            stopScanCommand
        );
    }

    private static GapAdvertisement OnAdvertisementReport(
        BleObserver bleObserver,
        HciLeExtendedAdvertisingReport report
    )
    {
        GapAdvertisement advertisement = GapAdvertisement.FromExtendedAdvertisingReport(
            bleObserver,
            DateTimeOffset.UtcNow,
            (BleEventType)report.EventType,
            new BleAddress((BleAddressType)report.AddressType, (UInt48)report.Address.ToUInt64()),
            (Physical)report.PrimaryPhy,
            (Physical)report.SecondaryPhy,
            (AdvertisingSId)report.AdvertisingSId,
            (TxPowerLevel)report.TxPower,
            (Rssi)report.Rssi,
            (PeriodicAdvertisingInterval)report.PeriodicAdvertisingInterval,
            new BleAddress((BleAddressType)report.DirectAddressType, (UInt48)report.DirectAddress.ToUInt64()),
            AdvertisingData.From(report.Data)
        );

        return advertisement;
    }
}
