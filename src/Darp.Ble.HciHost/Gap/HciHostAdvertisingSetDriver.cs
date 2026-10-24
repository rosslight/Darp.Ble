using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.HciHost.Gap;

/// <summary> One advertising set on an advertising handle of the controller. The core serializes the calls </summary>
internal sealed class HciHostAdvertisingSetDriver(
    HciDevice device,
    byte handle,
    int maxDataLength,
    IAdvertisingSetSink sink,
    Action releaseHandle
) : IAdvertisingSetDriver
{
    private readonly int _maxDataLength = maxDataLength;

    /// <summary> The controller's handle of the set </summary>
    public byte Handle { get; } = handle;

    /// <summary> Learns when a central connected through the set, which stopped it </summary>
    public IAdvertisingSetSink Sink { get; } = sink;

#pragma warning disable CA2213 // The adapter owns the device
    private readonly HciDevice _device = device;
#pragma warning restore CA2213
    private readonly byte _handle = handle;
    private readonly Action _releaseHandle = releaseHandle;
    private bool _created;

    public BleAddress RandomAddress { get; private set; } = BleAddress.NotAvailable;

    /// <summary> Creates the set on the controller with its parameters and address </summary>
    public async Task CreateAsync(
        AdvertisingParameters parameters,
        BleAddress address,
        CancellationToken cancellationToken
    )
    {
        await SetParametersAsync(parameters, cancellationToken).ConfigureAwait(false);
        _created = true;
        await SetRandomAddressAsync(address, cancellationToken).ConfigureAwait(false);
    }

    public Task StartAsync(CancellationToken cancellationToken) =>
        _device.RunAsync<HciLeSetExtendedAdvertisingEnableCommand, HciLeSetExtendedAdvertisingEnableResult>(
            Enable(enabled: true),
            "start advertising",
            cancellationToken
        );

    public Task StopAsync(CancellationToken cancellationToken) =>
        _device.RunAsync<HciLeSetExtendedAdvertisingEnableCommand, HciLeSetExtendedAdvertisingEnableResult>(
            Enable(enabled: false),
            "stop advertising",
            cancellationToken
        );

    /// <summary> The data must fit the controller and one command </summary>
    public static void CheckLength(AdvertisingData data, int maxLength, string name)
    {
        int length = data.AsReadOnlyMemory().Length;
        if (length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                name,
                length,
                $"The controller takes at most {maxLength} bytes of advertising data"
            );
        }
    }

    public Task SetParametersAsync(AdvertisingParameters parameters, CancellationToken cancellationToken)
    {
        return _device.RunAsync<
            HciLeSetExtendedAdvertisingParametersV1Command,
            HciLeSetExtendedAdvertisingParametersResult
        >(
            new HciLeSetExtendedAdvertisingParametersV1Command
            {
                AdvertisingHandle = _handle,
                AdvertisingEventProperties = (ushort)parameters.Type,
                PrimaryAdvertisingIntervalMin = HciTiming.ToAdvertisingUnits(
                    parameters.MinPrimaryAdvertisingInterval,
                    nameof(parameters.MinPrimaryAdvertisingInterval)
                ),
                PrimaryAdvertisingIntervalMax = HciTiming.ToAdvertisingUnits(
                    parameters.MaxPrimaryAdvertisingInterval,
                    nameof(parameters.MaxPrimaryAdvertisingInterval)
                ),
                PrimaryAdvertisingChannelMap = (byte)parameters.PrimaryAdvertisingChannelMap,
                OwnAddressType = 0x01,
                PeerAddressType = (byte?)parameters.PeerAddress?.Type ?? 0x00,
                PeerAddress = parameters.PeerAddress?.Value.ToUInt64() ?? 0x000000000000,
                AdvertisingFilterPolicy = (byte)parameters.FilterPolicy,
                AdvertisingTxPower = (byte)parameters.AdvertisingTxPower,
                PrimaryAdvertisingPhy = (byte)parameters.PrimaryPhy,
                SecondaryAdvertisingMaxSkip = 0,
                SecondaryAdvertisingPhy = (byte)parameters.SecondaryPhy,
                AdvertisingSid = (byte)parameters.AdvertisingSId,
                ScanRequestNotificationEnable = 0,
            },
            "set the advertising parameters",
            cancellationToken
        );
    }

    public Task SetDataAsync(AdvertisingData data, CancellationToken cancellationToken)
    {
        CheckLength(data, _maxDataLength, nameof(data));
        ReadOnlyMemory<byte> bytes = data.AsReadOnlyMemory();
        return _device.RunAsync<HciLeSetExtendedAdvertisingDataCommand, HciLeSetExtendedAdvertisingDataResult>(
            new HciLeSetExtendedAdvertisingDataCommand
            {
                AdvertisingHandle = _handle,
                Operation = 0x03, // Complete extended advertising data
                FragmentPreference = 0x01, // The controller should not fragment
                AdvertisingDataLength = (byte)bytes.Length,
                AdvertisingData = bytes,
            },
            "set the advertising data",
            cancellationToken
        );
    }

    public Task SetScanResponseDataAsync(AdvertisingData data, CancellationToken cancellationToken)
    {
        CheckLength(data, _maxDataLength, nameof(data));
        ReadOnlyMemory<byte> bytes = data.AsReadOnlyMemory();
        return _device.RunAsync<HciLeSetExtendedScanResponseDataCommand, HciLeSetExtendedScanResponseDataResult>(
            new HciLeSetExtendedScanResponseDataCommand
            {
                AdvertisingHandle = _handle,
                Operation = 0x03, // Complete scan response data
                FragmentPreference = 0x01, // The controller should not fragment
                ScanResponseDataLength = (byte)bytes.Length,
                ScanResponseData = bytes,
            },
            "set the scan response data",
            cancellationToken
        );
    }

    public async Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken)
    {
        await _device
            .RunAsync<HciLeSetAdvertisingSetRandomAddressCommand, HciLeSetAdvertisingSetRandomAddressResult>(
                new HciLeSetAdvertisingSetRandomAddressCommand
                {
                    AdvertisingHandle = _handle,
                    RandomAddress = address.Value.ToUInt64(),
                },
                "set the advertising address",
                cancellationToken
            )
            .ConfigureAwait(false);
        RandomAddress = address;
    }

    /// <summary> Removes the set from the controller; the core stopped it before </summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_created)
            {
                await _device
                    .RunAsync<HciLeRemoveAdvertisingSetCommand, HciLeRemoveAdvertisingSetResult>(
                        new HciLeRemoveAdvertisingSetCommand { AdvertisingHandle = _handle },
                        "remove the advertising set",
                        CancellationToken.None
                    )
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            _releaseHandle();
        }
    }

    private HciLeSetExtendedAdvertisingEnableCommand Enable(bool enabled) =>
        new()
        {
            Enable = enabled ? (byte)0x01 : (byte)0x00,
            NumSets = 1,
            AdvertisingHandle = new[] { _handle },
            Duration = new ushort[1],
            MaxExtendedAdvertisingEvents = new byte[1],
        };
}
