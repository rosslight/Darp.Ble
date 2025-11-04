using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Implementation;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;
using UInt48 = Darp.Ble.Data.UInt48;

namespace Darp.Ble.HciHost;

/// <inheritdoc cref="BleObserver" />
internal sealed partial class HciHostBleObserver : BleObserver
{
    private readonly HciHostBleDevice _device;
    private readonly IDisposable _subscription;

    private Hci.Host.HciHost Host => _device.HciDevice.Host;

    /// <inheritdoc />
    public HciHostBleObserver(HciHostBleDevice device, ILogger<HciHostBleObserver> logger)
        : base(device, logger)
    {
        _device = device;
        _subscription = Host.Subscribe(this);
    }

    [MessageSink]
    private void OnAdvertisementReport(HciLeExtendedAdvertisingReportEvent advEvent)
    {
        foreach (HciLeExtendedAdvertisingReport report in advEvent.Reports)
        {
            GapAdvertisement advertisement = GapAdvertisement.FromExtendedAdvertisingReport(
                this,
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
            OnNext(advertisement);
        }
    }

    private static (HciLeSetExtendedScanParametersCommand, HciLeSetExtendedScanEnableCommand) CreateConfiguration(
        BleObservationParameters parameters
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

    protected override async Task StartObservingAsyncCore(CancellationToken cancellationToken)
    {
        (HciLeSetExtendedScanParametersCommand Parameters, HciLeSetExtendedScanEnableCommand Enable) commands =
            CreateConfiguration(Parameters);

        HciSetExtendedScanParametersResult paramSetResult = await Host.QueryCommandCompletionAsync<
            HciLeSetExtendedScanParametersCommand,
            HciSetExtendedScanParametersResult
        >(commands.Parameters, cancellationToken)
            .ConfigureAwait(false);
        if (paramSetResult.Status is not HciCommandStatus.Success)
        {
            throw new BleObservationStartException(this, $"Could not set scan parameters: {paramSetResult.Status}");
        }

        HciSetExtendedScanEnableResult enableResult = await Host.QueryCommandCompletionAsync<
            HciLeSetExtendedScanEnableCommand,
            HciSetExtendedScanEnableResult
        >(commands.Enable, cancellationToken)
            .ConfigureAwait(false);
        if (enableResult.Status is not HciCommandStatus.Success)
        {
            throw new BleObservationStartException(this, $"Could not enable scan: {enableResult.Status}");
        }
    }

    /// <inheritdoc />
    protected override async Task StopObservingAsyncCore()
    {
        var stopScanCommand = new HciLeSetExtendedScanEnableCommand
        {
            Enable = 0x00,
            FilterDuplicates = 0x00,
            Duration = 0x0000,
            Period = 0x0000,
        };
        await Host.QueryCommandCompletionAsync<HciLeSetExtendedScanEnableCommand, HciSetExtendedScanEnableResult>(
                stopScanCommand
            )
            .ConfigureAwait(false);
    }

    protected override ValueTask DisposeAsyncCore()
    {
        _subscription.Dispose();
        return base.DisposeAsyncCore();
    }
}
