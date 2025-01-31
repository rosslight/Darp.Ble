using Darp.Ble.Data;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Att;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt.Server;

internal sealed class HciHostGattServerDescriptor(HciHostGattServerPeer serverPeer,
    BleUuid uuid,
    ushort attHandle,
    ILogger<HciHostGattServerDescriptor> logger)
{
    private readonly HciHostGattServerPeer _serverPeer = serverPeer;
    private readonly BleUuid _uuid = uuid;
    private ushort AttHandle { get; } = attHandle;
    private readonly ILogger<HciHostGattServerDescriptor> _logger = logger;

    public void WriteWithoutResponse(byte[] bytes)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bytes.Length, _serverPeer.AttMtu, nameof(bytes));
        _serverPeer.SendAttMtuCommand(new AttWriteCmd
        {
            Handle = AttHandle,
            Value = bytes,
        });
    }

    public async Task<bool> WriteAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        AttReadResult response = await _serverPeer.QueryAttPduAsync<AttWriteReq, AttWriteRsp>(
            new AttWriteReq
            {
                Handle = AttHandle,
                Value = bytes,
            }, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (response.OpCode is AttOpCode.ATT_ERROR_RSP
            && AttErrorRsp.TryReadLittleEndian(response.Pdu, out AttErrorRsp errorRsp, out _))
        {
            _logger.LogWarning("Could not write with response: {ErrorCode}", errorRsp.ErrorCode);
            return false;
        }
        if (!(response.OpCode is AttOpCode.ATT_WRITE_RSP && AttWriteRsp.TryReadLittleEndian(response.Pdu, out AttWriteRsp _)))
        {
            _logger.LogWarning("Received unexpected att response {OpCode}", response.OpCode);
            return false;
        }
        return true;
    }
}