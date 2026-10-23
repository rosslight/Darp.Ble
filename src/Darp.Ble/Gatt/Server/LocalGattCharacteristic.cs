using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A characteristic the adapter hosts, as added to a <see cref="LocalGattServiceBuilder"/> </summary>
public sealed class LocalGattCharacteristic
{
    private readonly OnReadHandler? _onRead;
    private readonly OnWriteHandler? _onWrite;

    // The peripheral that hosts the characteristic, once its service is live
    private BlePeripheral? _host;

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

    /// <summary> Sends a notification to a central </summary>
    /// <param name="central"> A central connected to the adapter that hosts the characteristic </param>
    /// <param name="value"> The value; the caller may reuse the buffer once the task completed </param>
    /// <param name="cancellationToken"> Cancels waiting to send; once the backend accepted the notification, only the wait ends </param>
    /// <returns> A task that completes once the backend accepted the notification; not when the central got it </returns>
    /// <exception cref="InvalidOperationException"> The characteristic does not notify, its service was not added, or the central belongs to another adapter </exception>
    /// <exception cref="BleDisconnectedException"> The central's connection ended </exception>
    public ValueTask NotifyAsync(
        ConnectedCentral central,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(central);
        if (!Properties.HasFlag(GattProperty.Notify))
            throw new InvalidOperationException($"The characteristic {Uuid} does not notify");
        BlePeripheral? host = _host;
        if (host is null)
            throw new InvalidOperationException($"The characteristic {Uuid} is not live; add its service first");
        if (!ReferenceEquals(central.Peripheral, host))
            throw new InvalidOperationException("The central is connected to another adapter");
        return central.NotifyAsync(Definition, value, cancellationToken);
    }

    /// <summary> The service is live on this peripheral </summary>
    internal void MarkLive(BlePeripheral host) => _host = host;

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
