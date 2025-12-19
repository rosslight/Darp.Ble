using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload.Att;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt.Server;

internal sealed class HciHostGattServerDescriptor(
    HciHostGattServerCharacteristic characteristic,
    BleUuid uuid,
    ushort attributeHandle,
    ILogger<HciHostGattServerDescriptor> logger
) : GattServerDescriptor(characteristic, uuid, logger)
{
    private readonly HciHostGattServerPeer _peer = characteristic.Service.Peer;
    internal ushort AttributeHandle { get; } = attributeHandle;

    public void WriteWithoutResponse(byte[] bytes)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bytes.Length, _peer.AttMtu, nameof(bytes));
        _peer.Connection.EnqueueGattPacket(new AttWriteCmd { Handle = AttributeHandle, Value = bytes }, activity: null);
    }

    public override async Task<bool> WriteAsync(byte[] bytes, CancellationToken cancellationToken = default)
    {
        AttResponse<AttWriteRsp> response = await _peer
            .Connection.QueryAttPduAsync<AttWriteReq, AttWriteRsp>(
                new AttWriteReq { AttributeHandle = AttributeHandle, AttributeValue = bytes },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        if (response.IsError)
        {
            Logger.LogWarning("Could not write with response: {ErrorCode}", response.Error.ErrorCode);
            return false;
        }
        return true;
    }

    public override Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
