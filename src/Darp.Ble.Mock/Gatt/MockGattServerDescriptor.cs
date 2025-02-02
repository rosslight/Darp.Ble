using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattServerDescriptor(
    MockGattServerCharacteristic characteristic,
    BleUuid uuid,
    MockGattClientDescriptor mockDescriptor,
    MockGattClientPeer clientPeer,
    ILogger<MockGattServerDescriptor> logger
) : GattServerDescriptor(characteristic, uuid, logger)
{
    private readonly MockGattClientDescriptor _mockDescriptor = mockDescriptor;
    private readonly MockGattClientPeer _clientPeer = clientPeer;

    public override async Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
    {
        return await _mockDescriptor
            .GetValueAsync(_clientPeer, cancellationToken)
            .ConfigureAwait(false);
    }

    public override async Task<bool> WriteAsync(
        byte[] bytes,
        CancellationToken cancellationToken = default
    )
    {
        GattProtocolStatus result = await _mockDescriptor
            .UpdateValueAsync(_clientPeer, bytes, cancellationToken)
            .ConfigureAwait(false);
        return result is GattProtocolStatus.Success;
    }
}
