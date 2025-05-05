using System.Collections.Concurrent;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Implementation;
using Darp.Ble.Utils;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

/// <summary> The hci implementation of a <see cref="IBleBroadcaster"/> </summary>
/// <param name="device"> The device </param>
/// <param name="logger"> An optional logger </param>
internal sealed class HciHostBleBroadcaster(
    HciHostBleDevice device,
    ushort maxAdvertisingDataLength,
    ILogger<HciHostBleBroadcaster> logger
) : BleBroadcaster(device, logger)
{
    private readonly HciHostBleDevice _device = device;
    private readonly ConcurrentDictionary<byte, HciAdvertisingSet> _advertisingSets = [];

    public ushort MaxAdvertisingDataLength { get; } = maxAdvertisingDataLength;
    public Hci.HciHost Host => _device.Host;

    private async Task RegisterAdvertisingSetAsync(
        HciAdvertisingSet advertisingSet,
        CancellationToken cancellationToken
    )
    {
        HciLeReadNumberOfSupportedAdvertisingSetsResult result = await Host.QueryCommandCompletionAsync<
            HciLeReadNumberOfSupportedAdvertisingSetsCommand,
            HciLeReadNumberOfSupportedAdvertisingSetsResult
        >(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        for (byte handle = 0; handle < result.NumSupportedAdvertisingSets; handle++)
        {
            if (!_advertisingSets.TryAdd(handle, advertisingSet))
                continue;

#pragma warning disable CS0618 // Type or member is obsolete - We are registering the handle here.
            advertisingSet.AdvertisingHandle = handle;
#pragma warning restore CS0618
            return;
        }
        throw new ArgumentOutOfRangeException(
            nameof(advertisingSet),
            "Cannot add additional advertising set. There are not enough"
        );
    }

    /// <inheritdoc />
    protected override async Task<IAdvertisingSet> CreateAdvertisingSetAsyncCore(
        AdvertisingParameters? parameters,
        AdvertisingData? data,
        AdvertisingData? scanResponseData,
        CancellationToken cancellationToken
    )
    {
        parameters ??= AdvertisingParameters.Default;

        var set = new HciAdvertisingSet(this);
        await RegisterAdvertisingSetAsync(set, cancellationToken).ConfigureAwait(false);
        try
        {
            await set.SetAdvertisingParametersAsync(parameters, cancellationToken).ConfigureAwait(false);
            await set.SetRandomAddressAsync(_device.RandomAddress, cancellationToken).ConfigureAwait(false);
            if (data?.Count > 0)
            {
                await set.SetAdvertisingDataAsync(data, cancellationToken).ConfigureAwait(false);
            }
            if (scanResponseData is not null)
            {
                await set.SetScanResponseDataAsync(scanResponseData, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            await set.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return set;
    }

    /// <inheritdoc />
    protected override async Task<IAsyncDisposable> StartAdvertisingCoreAsync(
        IReadOnlyCollection<(IAdvertisingSet AdvertisingSet, TimeSpan Duration, byte NumberOfEvents)> advertisingSets,
        CancellationToken cancellationToken
    )
    {
        var advertisingHandleArray = new byte[advertisingSets.Count];
        var durationArray = new ushort[advertisingSets.Count];
        var numberOfEventsArray = new byte[advertisingSets.Count];
        var i = 0;
        foreach ((IAdvertisingSet advertisingSet, TimeSpan duration, byte numberOfEvents) in advertisingSets)
        {
            if (advertisingSet is not HciAdvertisingSet hciAdvertisingSet)
                throw new ArgumentException(
                    "An advertising set is not of type HciAdvertisingSet. This should not happen",
                    nameof(advertisingSets)
                );
            advertisingHandleArray[i] = hciAdvertisingSet.AdvertisingHandle;
            durationArray[i] = (ushort)(duration.TotalMilliseconds / 10);
            numberOfEventsArray[i] = numberOfEvents;
            i++;
        }
        HciLeSetExtendedAdvertisingEnableResult enableResult = await Host.QueryCommandCompletionAsync<
            HciLeSetExtendedAdvertisingEnableCommand,
            HciLeSetExtendedAdvertisingEnableResult
        >(
                new HciLeSetExtendedAdvertisingEnableCommand
                {
                    Enable = 0x01,
                    NumSets = (byte)advertisingSets.Count,
                    AdvertisingHandle = advertisingHandleArray,
                    Duration = durationArray,
                    MaxExtendedAdvertisingEvents = numberOfEventsArray,
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        if (enableResult.Status is not HciCommandStatus.Success)
        {
            throw new BleBroadcasterException(
                this,
                $"Could not enable advertising sets because of {enableResult.Status}"
            );
        }
        foreach (var tuple in advertisingSets)
        {
            if (tuple.AdvertisingSet is HciAdvertisingSet hciAdvertisingSet)
                hciAdvertisingSet.SetAdvertisingStatus(isEnabled: true);
        }
        return AsyncDisposable.Create(async () =>
        {
            HciLeSetExtendedAdvertisingEnableResult result = await Host.QueryCommandCompletionAsync<
                HciLeSetExtendedAdvertisingEnableCommand,
                HciLeSetExtendedAdvertisingEnableResult
            >(
                    new HciLeSetExtendedAdvertisingEnableCommand
                    {
                        Enable = 0x00,
                        NumSets = (byte)advertisingSets.Count,
                        AdvertisingHandle = advertisingHandleArray,
                        Duration = durationArray,
                        MaxExtendedAdvertisingEvents = numberOfEventsArray,
                    },
                    cancellationToken: cancellationToken
                )
                .ConfigureAwait(false);
            if (result.Status is HciCommandStatus.Success)
            {
                foreach (var tuple in advertisingSets)
                {
                    if (tuple.AdvertisingSet is HciAdvertisingSet hciAdvertisingSet)
                        hciAdvertisingSet.SetAdvertisingStatus(isEnabled: false);
                }
            }
        });
    }

    protected override async Task<bool> StopAdvertisingCoreAsync(
        IReadOnlyCollection<IAdvertisingSet> advertisingSets,
        CancellationToken cancellationToken
    )
    {
        byte[] advertisingHandles = advertisingSets
            .OfType<HciAdvertisingSet>()
            .Select(x => x.AdvertisingHandle)
            .ToArray();
        HciLeSetExtendedAdvertisingEnableResult result = await Host.QueryCommandCompletionAsync<
            HciLeSetExtendedAdvertisingEnableCommand,
            HciLeSetExtendedAdvertisingEnableResult
        >(
                new HciLeSetExtendedAdvertisingEnableCommand
                {
                    Enable = 0x00,
                    NumSets = (byte)advertisingHandles.Length,
                    AdvertisingHandle = advertisingHandles,
                    Duration = new ushort[advertisingHandles.Length],
                    MaxExtendedAdvertisingEvents = new byte[advertisingHandles.Length],
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        if (result.Status is not HciCommandStatus.Success)
            return false;
        foreach (var set in advertisingSets)
        {
            if (set is HciAdvertisingSet hciAdvertisingSet)
                hciAdvertisingSet.SetAdvertisingStatus(isEnabled: false);
        }
        return true;
    }
}
