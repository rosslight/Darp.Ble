using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Att;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt.Server;

internal sealed class HciHostGattServerDescriptor(HciHostGattServerCharacteristic characteristic,
    BleUuid uuid,
    ushort attHandle,
    ILogger<HciHostGattServerDescriptor> logger) : GattServerDescriptor(characteristic, uuid, logger)
{
    private readonly HciHostGattServerPeer _peer = characteristic.Service.Peer;
    private ushort AttHandle { get; } = attHandle;

    public void WriteWithoutResponse(byte[] bytes)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bytes.Length, _peer.AttMtu, nameof(bytes));
        _peer.SendAttMtuCommand(new AttWriteCmd
        {
            Handle = AttHandle,
            Value = bytes,
        });
    }

    public async Task<bool> WriteAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        AttReadResult response = await _peer.QueryAttPduAsync<AttWriteReq, AttWriteRsp>(
            new AttWriteReq
            {
                Handle = AttHandle,
                Value = bytes,
            }, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (response.OpCode is AttOpCode.ATT_ERROR_RSP
            && AttErrorRsp.TryReadLittleEndian(response.Pdu, out AttErrorRsp errorRsp, out _))
        {
            Logger.LogWarning("Could not write with response: {ErrorCode}", errorRsp.ErrorCode);
            return false;
        }
        if (!(response.OpCode is AttOpCode.ATT_WRITE_RSP && AttWriteRsp.TryReadLittleEndian(response.Pdu, out AttWriteRsp _)))
        {
            Logger.LogWarning("Received unexpected att response {OpCode}", response.OpCode);
            return false;
        }
        return true;
    }
}