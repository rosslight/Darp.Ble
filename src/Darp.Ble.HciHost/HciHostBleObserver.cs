using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

/// <inheritdoc />
public sealed class HciHostBleObserver(HciHostBleDevice device, ILogger? logger) : BleObserver(device, logger)
{
    private readonly HciHostBleDevice _device = device;
    private static (HciSetExtendedScanParametersCommand, HciSetExtendedScanEnableCommand) CreateConfiguration(BleScanParameters parameters)
    {
        bool isInActiveMode = parameters.ScanType is ScanType.Active;
        return (new HciSetExtendedScanParametersCommand
        {
            OwnDeviceAddress = 0x01,
            ScanningFilterPolicy = 0x00,
            ScanPhys = 0b00000001,
            ScanType = isInActiveMode ? (byte)0x01 : (byte)0x00,
            ScanInterval = (ushort)parameters.ScanInterval,
            ScanWindow = (ushort)parameters.ScanWindow,
        }, new HciSetExtendedScanEnableCommand
        {
            Enable = 0x01,
            FilterDuplicates = 0x00,
            Duration = 0x0000,
            Period = 0x0000,
        });
    }

    /// <inheritdoc />
    protected override bool TryStartScanCore(out IObservable<IGapAdvertisement> observable)
    {
        (HciSetExtendedScanParametersCommand Parameters, HciSetExtendedScanEnableCommand Enable) commands = CreateConfiguration(Parameters);
        //Logger.Verbose("AdvertisingScanner: Using scan params {@ScanParams} and enable params {@EnableParams}", commands.Parameters, commands.Enable);
        observable = Observable.Create<IGapAdvertisement>(async (observer, token) =>
        {
            HciSetExtendedScanParametersResult paramSetResult = await _device.Host
                .QueryCommandCompletionAsync<HciSetExtendedScanParametersCommand, HciSetExtendedScanParametersResult>(
                    commands.Parameters, cancellationToken: token);
            if (paramSetResult.Status is not HciCommandStatus.Success)
            {
                observer.OnError(new BleObservationStartException(this, $"Could not set scan parameters: {paramSetResult.Status}"));
                return;
            }
            HciSetExtendedScanEnableResult enableResult = await _device.Host.QueryCommandCompletionAsync<HciSetExtendedScanEnableCommand, HciSetExtendedScanEnableResult>(commands.Enable, cancellationToken: token);
            if (enableResult.Status is not HciCommandStatus.Success)
            {
                observer.OnError(new BleObservationStartException(this, $"Could not enable scan: {enableResult.Status}"));
                return;
            }

            _device.Host
                .WhenHciEventPackageReceived
                .SelectWhereEvent<HciLeExtendedAdvertisingReportEvent>()
                .SelectMany(x => x.Data.Reports)
                .Select(x => OnAdvertisementReport(this, x))
                .Subscribe(observer, token);
            await Task.Delay(TimeSpan.MaxValue, token);
        });
        return true;
    }

    /// <inheritdoc />
    protected override void StopScanCore()
    {
        var stopScanCommand = new HciSetExtendedScanEnableCommand
        {
            Enable = 0x00,
            FilterDuplicates = 0x00,
            Duration = 0x0000,
            Period = 0x0000,
        };
        _ = _device.Host.QueryCommandCompletionAsync<HciSetExtendedScanEnableCommand, HciSetExtendedScanEnableResult>(stopScanCommand);
    }

    private static GapAdvertisement OnAdvertisementReport(BleObserver bleObserver, HciLeExtendedAdvertisingReport report)
    {
        GapAdvertisement advertisement = GapAdvertisement.FromExtendedAdvertisingReport(bleObserver,
            DateTimeOffset.UtcNow,
            (BleEventType)report.EventType,
            new BleAddress((BleAddressType)report.AddressType, (UInt48)report.Address.Address),
            (Physical)report.PrimaryPhy,
            (Physical)report.SecondaryPhy,
            (AdvertisingSId)report.AdvertisingSId,
            (TxPowerLevel)report.TxPower,
            (Rssi)report.Rssi,
            (PeriodicAdvertisingInterval)report.PeriodicAdvertisingInterval,
            new BleAddress((BleAddressType)report.DirectAddressType, (UInt48)report.DirectAddress.Address),
            AdvertisingData.From(report.Data));

        return advertisement;
    }
}