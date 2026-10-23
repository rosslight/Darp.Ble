using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A descriptor the adapter hosts, as added to a <see cref="LocalGattCharacteristic"/> </summary>
public sealed class LocalGattDescriptor : IHostedAttribute
{
    private readonly OnReadHandler? _onRead;
    private readonly OnWriteHandler? _onWrite;

    internal LocalGattDescriptor(GattDescriptorDefinition definition, OnReadHandler? onRead, OnWriteHandler? onWrite)
    {
        Definition = definition;
        _onRead = onRead;
        _onWrite = onWrite;
    }

    /// <summary> The UUID of the descriptor </summary>
    public BleUuid Uuid => Definition.Uuid;

    internal GattDescriptorDefinition Definition { get; }

    GattAttributeDefinition IHostedAttribute.Definition => Definition;

    ValueTask<byte[]> IHostedAttribute.ReadAsync(ConnectedCentral central, CancellationToken cancellationToken) =>
        _onRead is null
            ? throw new GattException(GattProtocolStatus.ReadNotPermitted, "The descriptor has no read handler")
            : _onRead(central, cancellationToken);

    ValueTask<GattProtocolStatus> IHostedAttribute.WriteAsync(
        ConnectedCentral central,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    ) =>
        _onWrite is null
            ? throw new GattException(GattProtocolStatus.WriteNotPermitted, "The descriptor has no write handler")
            : _onWrite(central, value, cancellationToken);
}
