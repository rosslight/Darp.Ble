using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Att;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt.Server;

public sealed class HciHostGattServerDescriptor(HciHostGattServerPeer serverPeer, BleUuid uuid, ushort attHandle, ILogger? logger)
{
    private readonly HciHostGattServerPeer _serverPeer = serverPeer;
    private readonly BleUuid _uuid = uuid;
    private ushort AttHandle { get; } = attHandle;
    private readonly ILogger? _logger = logger;

    public void OnWrite(byte[] bytes, CancellationToken token)
    {
        _serverPeer.SendAttMtuCommand(new AttWriteCmd
        {
            Handle = AttHandle,
            Value = bytes,
        }, token: token);
    }

    public async Task<bool> WriteWithResponseAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        AttReadResult response = await _serverPeer.QueryAttPduAsync<AttWriteReq, AttWriteRsp>(
            new AttWriteReq
            {
                Handle = AttHandle,
                Value = bytes,
            }, cancellationToken: cancellationToken);
        if (response.OpCode is AttOpCode.ATT_ERROR_RSP
            && AttErrorRsp.TryDecode(response.Pdu, out AttErrorRsp errorRsp, out _))
        {
            _logger?.LogWarning("Could not write with response: {ErrorCode}", errorRsp.ErrorCode);
            return false;
        }
        if (!(response.OpCode is AttOpCode.ATT_WRITE_RSP
              && AttWriteRsp.TryDecode(response.Pdu, out AttWriteRsp rsp, out _)))
        {
            _logger?.LogWarning("Received unexpected att response {OpCode}", response.OpCode);
            return false;
        }
        return true;
    }
}