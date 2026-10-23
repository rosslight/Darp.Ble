using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A characteristic the adapter hosts, as added to a <see cref="LocalGattServiceBuilder"/> </summary>
public sealed class LocalGattCharacteristic
{
    private readonly OnReadHandler? _onRead;
    private readonly OnWriteHandler? _onWrite;

    internal LocalGattCharacteristic(
        GattCharacteristicDefinition definition,
        OnReadHandler? onRead,
        OnWriteHandler? onWrite
    )
    {
        Definition = definition;
        _onRead = onRead;
        _onWrite = onWrite;
    }

    /// <summary> The UUID of the characteristic </summary>
    public BleUuid Uuid => Definition.Uuid;

    /// <summary> What the characteristic supports </summary>
    public GattProperty Properties => Definition.Properties;

    internal GattCharacteristicDefinition Definition { get; }

    /// <summary> Answers a read of a central, through its handler </summary>
    internal ValueTask<byte[]> ReadAsync(ConnectedCentral central, CancellationToken cancellationToken) =>
        _onRead is null
            ? throw new GattException(GattProtocolStatus.ReadNotPermitted, "The characteristic has no read handler")
            : _onRead(central, cancellationToken);

    /// <summary> Handles a write of a central, through its handler </summary>
    internal ValueTask<GattProtocolStatus> WriteAsync(
        ConnectedCentral central,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    ) =>
        _onWrite is null
            ? throw new GattException(GattProtocolStatus.WriteNotPermitted, "The characteristic has no write handler")
            : _onWrite(central, value, cancellationToken);
}
