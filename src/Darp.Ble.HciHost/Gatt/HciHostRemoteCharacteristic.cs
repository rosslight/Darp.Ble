using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Connections;

namespace Darp.Ble.HciHost.Gatt;

/// <summary> A characteristic of the peer: its value handle and the handles of its descriptors up to its end </summary>
internal sealed class HciHostRemoteCharacteristic(
    HciHostConnectionDriver connection,
    ushort valueHandle,
    ushort endHandle,
    BleUuid uuid,
    GattProperty properties
) : IRemoteCharacteristicDriver
{
    private static readonly BleUuid CccdUuid = BleUuid.FromUInt16(0x2902);

    private readonly HciHostConnectionDriver _connection = connection;
    private readonly ushort _endHandle = endHandle;
    private IReadOnlyList<HciHostRemoteDescriptor>? _descriptors;

    public ushort ValueHandle { get; } = valueHandle;

    public BleUuid Uuid { get; } = uuid;

    public GattProperty Properties { get; } = properties;

    public Task<byte[]> ReadAsync(GattRequest request) => _connection.ReadAsync(ValueHandle, request);

    public Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request) =>
        _connection.WriteAsync(ValueHandle, value, request);

    public ValueTask WriteWithoutResponseAsync(
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken,
        CancellationToken connectionClosed
    ) => _connection.WriteCommandAsync(ValueHandle, value, cancellationToken, connectionClosed);

    public async Task<IReadOnlyList<IRemoteDescriptorDriver>> DiscoverDescriptorsAsync(GattRequest request) =>
        await GetDescriptorsAsync(request).ConfigureAwait(false);

    /// <summary> Writes the CCCD; finds it first if the descriptors were not discovered yet </summary>
    public async Task SetNotificationsAsync(bool enabled, GattRequest request)
    {
        IReadOnlyList<HciHostRemoteDescriptor> descriptors = await GetDescriptorsAsync(request).ConfigureAwait(false);
        // The caller gave up while the CCCD was looked up: nothing was written, so this is no success
        if (request.IsAbandoned)
            throw new OperationCanceledException("Notifications were not changed; the request was abandoned");
        HciHostRemoteDescriptor cccd =
            descriptors.FirstOrDefault(descriptor => descriptor.Uuid == CccdUuid)
            ?? throw new InvalidOperationException(
                $"The characteristic {Uuid} has no client characteristic configuration descriptor"
            );
        await _connection
            .WriteAsync(cccd.Handle, enabled ? new byte[] { 0x01, 0x00 } : new byte[] { 0x00, 0x00 }, request)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<HciHostRemoteDescriptor>> GetDescriptorsAsync(GattRequest request)
    {
        if (_descriptors is { } known)
            return known;
        List<HciHostRemoteDescriptor> descriptors = [];
        var start = (ushort)(ValueHandle + 1);
        while (start <= _endHandle && start > ValueHandle && !request.IsAbandoned)
        {
            AttFindInformationRsp? page = await _connection
                .Client.QueryUnlessNotFoundAsync<AttFindInformationReq, AttFindInformationRsp>(
                    new AttFindInformationReq { StartingHandle = start, EndingHandle = _endHandle },
                    request
                )
                .ConfigureAwait(false);
            if (page is not { InformationData.Length: > 0 } response)
                break;
            foreach ((ushort handle, ReadOnlyMemory<byte> uuid) in response.InformationData.Span)
                descriptors.Add(_connection.GetDescriptor(handle, BleUuid.Read(uuid.Span)));
            ushort last = response.InformationData.Span[^1].Handle;
            if (last >= _endHandle)
                break;
            start = (ushort)(last + 1);
        }
        // An abandoned discovery may be incomplete, so the next one asks again
        if (!request.IsAbandoned)
            _descriptors = descriptors;
        return descriptors;
    }
}
