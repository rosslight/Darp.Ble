using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Implementation;

namespace Darp.Ble.HciHost;

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
