using System.Collections;
using System.Collections.Concurrent;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.HciHost.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.Utils;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

internal sealed class HciHostGattClientService(
    HciHostBlePeripheral peripheral,
    BleUuid uuid,
    GattServiceType type,
    ILogger<HciHostGattClientService> logger
) : GattClientService(peripheral, uuid, type, logger)
{
    protected override GattClientCharacteristic CreateCharacteristicCore(
        BleUuid uuid,
        GattProperty gattProperty,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite
    )
    {
        return new HciHostGattClientCharacteristic(
            this,
            uuid,
            gattProperty,
            onRead,
            onWrite,
            LoggerFactory.CreateLogger<HciHostGattClientCharacteristic>()
        );
    }
}

internal sealed class HciHostGattClientCharacteristic(
    GattClientService clientService,
    BleUuid uuid,
    GattProperty gattProperty,
    IGattClientAttribute.OnReadCallback? onRead,
    IGattClientAttribute.OnWriteCallback? onWrite,
    ILogger<HciHostGattClientCharacteristic> logger
) : GattClientCharacteristic(clientService, uuid, gattProperty, onRead, onWrite, logger)
{
    protected override GattClientDescriptor AddDescriptorCore(
        BleUuid uuid,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite
    )
    {
        return new HciHostGattClientDescriptor(this, uuid, onRead, onWrite);
    }

    protected override void NotifyCore(IGattClientPeer clientPeer, byte[] value)
    {
        throw new NotImplementedException();
    }

    protected override Task IndicateAsyncCore(
        IGattClientPeer clientPeer,
        byte[] value,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }
}

internal sealed class HciHostGattClientDescriptor(
    HciHostGattClientCharacteristic clientCharacteristic,
    BleUuid uuid,
    IGattClientAttribute.OnReadCallback? onRead,
    IGattClientAttribute.OnWriteCallback? onWrite
) : GattClientDescriptor(clientCharacteristic, uuid, onRead, onWrite);

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
    private readonly ConcurrentDictionary<byte, IAdvertisingSet> _advertisingSets = [];

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
                await set.SetAdvertisingParametersAsync(parameters, cancellationToken).ConfigureAwait(false);
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
        await Host.QueryCommandCompletionAsync<
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
        return AsyncDisposable.Create(async () =>
        {
            await Host.QueryCommandCompletionAsync<
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
        });
    }
}

internal sealed class HciAdvertisingSet(HciHostBleBroadcaster broadcaster) : AdvertisingSet(broadcaster)
{
    private readonly Hci.HciHost _host = broadcaster.Host;

    /// <summary> The handle of the advertising set </summary>
    public byte AdvertisingHandle
    {
        get;
        [Obsolete("This should not be used after the set was registered")]
        internal set;
    }

    /// <inheritdoc />
    public override async Task SetRandomAddressAsync(
        BleAddress randomAddress,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(randomAddress);
        await _host
            .QueryCommandCompletionAsync<
                HciLeSetAdvertisingSetRandomAddressCommand,
                HciLeSetAdvertisingSetRandomAddressResult
            >(
                new HciLeSetAdvertisingSetRandomAddressCommand
                {
                    AdvertisingHandle = AdvertisingHandle,
                    RandomAddress = randomAddress.Value.ToUInt64(),
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task SetAdvertisingParametersAsync(
        AdvertisingParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(parameters);
        HciLeSetExtendedAdvertisingParametersResult result = await _host
            .QueryCommandCompletionAsync<
                HciLeSetExtendedAdvertisingParametersV1Command,
                HciLeSetExtendedAdvertisingParametersResult
            >(
                new HciLeSetExtendedAdvertisingParametersV1Command
                {
                    AdvertisingHandle = AdvertisingHandle,
                    AdvertisingEventProperties = (ushort)parameters.Type,
                    PrimaryAdvertisingIntervalMin = (uint)parameters.MinPrimaryAdvertisingInterval,
                    PrimaryAdvertisingIntervalMax = (uint)parameters.MaxPrimaryAdvertisingInterval,
                    PrimaryAdvertisingChannelMap = (byte)parameters.PrimaryAdvertisingChannelMap,
                    OwnAddressType = (byte)BleAddressType.RandomStatic,
                    PeerAddressType = (byte?)parameters.PeerAddress?.Type ?? 0x00,
                    PeerAddress = parameters.PeerAddress?.Value.ToUInt64() ?? 0x000000000000,
                    AdvertisingFilterPolicy = (byte)parameters.FilterPolicy,
                    AdvertisingTxPower = (byte)parameters.AdvertisingTxPower,
                    PrimaryAdvertisingPhy = (byte)parameters.PrimaryPhy,
                    SecondaryAdvertisingMaxSkip = 0,
                    SecondaryAdvertisingPhy = (byte)Physical.Le1M,
                    AdvertisingSid = (byte)parameters.AdvertisingSId,
                    ScanRequestNotificationEnable = 0,
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        SelectedTxPower = (TxPowerLevel)result.SelectedTxPower;
        Parameters = parameters;
    }

    /// <inheritdoc />
    public override async Task SetAdvertisingDataAsync(
        AdvertisingData data,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(data);
        ReadOnlyMemory<byte> memory = data.AsReadOnlyMemory();
        if (memory.Length > 251)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Advertising data cannot be > 251 bytes right now");
        }
        await _host
            .QueryCommandCompletionAsync<HciLeSetExtendedAdvertisingDataCommand, HciLeSetExtendedAdvertisingDataResult>(
                new HciLeSetExtendedAdvertisingDataCommand
                {
                    AdvertisingHandle = AdvertisingHandle,
                    Operation = 0x03, // Complete extended advertising data
                    FragmentPreference = 0x01, // The Controller should not fragment or should minimize fragmentation of Host advertising data
                    AdvertisingDataLength = (byte)memory.Length,
                    AdvertisingData = memory,
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        Data = data;
    }

    /// <inheritdoc />
    public override async Task SetScanResponseDataAsync(
        AdvertisingData scanResponseData,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(scanResponseData);
        ReadOnlyMemory<byte> memory = scanResponseData.AsReadOnlyMemory();
        if (memory.Length > 251)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scanResponseData),
                "Scan response data cannot be > 251 bytes right now"
            );
        }
        await _host
            .QueryCommandCompletionAsync<
                HciLeSetExtendedScanResponseDataCommand,
                HciLeSetExtendedScanResponseDataResult
            >(
                new HciLeSetExtendedScanResponseDataCommand
                {
                    AdvertisingHandle = AdvertisingHandle,
                    Operation = 0x03, // Complete extended advertising data
                    FragmentPreference = 0x01, // The Controller should not fragment or should minimize fragmentation of Host advertising data
                    ScanResponseDataLength = (byte)memory.Length,
                    ScanResponseData = memory,
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        ScanResponseData = scanResponseData;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await base.DisposeAsyncCore().ConfigureAwait(false);
        await _host
            .QueryCommandCompletionAsync<HciLeRemoveAdvertisingSetCommand, HciLeRemoveAdvertisingSetResult>(
                new HciLeRemoveAdvertisingSetCommand { AdvertisingHandle = AdvertisingHandle },
                cancellationToken: CancellationToken.None
            )
            .ConfigureAwait(false);
    }
}
