using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> An abstract gatt client characteristic </summary>
/// <param name="clientCharacteristic"> The parent client characteristic </param>
/// <param name="uuid"> The UUID of the characteristic </param>
/// <param name="onRead"> The callback to be called when a read operation was requested on this attribute </param>
/// <param name="onWrite"> The callback to be called when a write operation was requested on this attribute </param>
public abstract class GattClientDescriptor(
    GattClientCharacteristic clientCharacteristic,
    BleUuid uuid,
    IGattClientAttribute.OnReadCallback? onRead,
    IGattClientAttribute.OnWriteCallback? onWrite) : IGattClientDescriptor
{
    private readonly IGattClientAttribute.OnReadCallback? _onRead = onRead;
    private readonly IGattClientAttribute.OnWriteCallback? _onWrite = onWrite;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;
    /// <inheritdoc />
    public IGattClientCharacteristic Characteristic { get; } = clientCharacteristic;

    /// <inheritdoc />
    public ValueTask<byte[]> GetValueAsync(IGattClientPeer? clientPeer, CancellationToken cancellationToken)
    {
        if (_onRead is null)
            throw new NotSupportedException("Reading is not supported by this descriptor");
        return _onRead(clientPeer, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<GattProtocolStatus> UpdateValueAsync(IGattClientPeer? clientPeer, byte[] value, CancellationToken cancellationToken)
    {
        if (_onWrite is null)
            throw new NotSupportedException("Writing is not supported by this descriptor");
        return _onWrite(clientPeer, value, cancellationToken);
    }
}