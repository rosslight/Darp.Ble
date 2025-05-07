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
    private Hci.HciHost Host => _device.Host;

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

    protected override async Task<IDisposable> StartObservingAsyncCore<TState>(
        TState state,
        Action<TState, IGapAdvertisement> onAdvertisement,
        CancellationToken cancellationToken
    )
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

        return Host.AsObservable<HciLeExtendedAdvertisingReportEvent>()
            .SelectMany(x => x.Reports)
            .Select(x => OnAdvertisementReport(this, x))
            .Subscribe(advertisement => onAdvertisement(state, advertisement));
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
