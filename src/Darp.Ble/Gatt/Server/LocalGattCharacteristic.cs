using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> A characteristic the adapter hosts, as added to a <see cref="LocalGattServiceBuilder"/> </summary>
public sealed class LocalGattCharacteristic : IHostedAttribute
{
    private readonly LocalGattServiceBuilder _builder;
    private readonly OnReadHandler? _onRead;
    private readonly OnWriteHandler? _onWrite;
    private readonly Lock _lock = new();
    private readonly List<LocalGattDescriptor> _descriptors = [];

    // The peripheral that hosts the characteristic; set while a publication runs or succeeded
    private BlePeripheral? _host;
    private bool _isLive;

    internal LocalGattCharacteristic(
        LocalGattServiceBuilder builder,
        GattCharacteristicDefinition definition,
        OnReadHandler? onRead,
        OnWriteHandler? onWrite
    )
    {
        _builder = builder;
        Definition = definition;
        _onRead = onRead;
        _onWrite = onWrite;
    }

    /// <summary> The UUID of the characteristic </summary>
    public BleUuid Uuid => Definition.Uuid;

    /// <summary> What the characteristic supports </summary>
    public GattProperty Properties => Definition.Properties;

    internal GattCharacteristicDefinition Definition { get; }

    GattAttributeDefinition IHostedAttribute.Definition => Definition;

    /// <summary> Adds a descriptor, while the service is being built </summary>
    /// <param name="uuid"> The UUID of the descriptor </param>
    /// <param name="onRead"> Answers reads of centrals; without it a read is answered with <see cref="GattProtocolStatus.ReadNotPermitted"/> </param>
    /// <param name="onWrite"> Handles writes of centrals; without it a write is answered with <see cref="GattProtocolStatus.WriteNotPermitted"/> </param>
    /// <returns> The descriptor </returns>
    /// <exception cref="InvalidOperationException"> The service was already published </exception>
    public LocalGattDescriptor AddDescriptor(BleUuid uuid, OnReadHandler? onRead = null, OnWriteHandler? onWrite = null)
    {
        ArgumentNullException.ThrowIfNull(uuid);
        return AddDescriptor(new GattDescriptorDefinition(uuid, staticValue: null), onRead, onWrite);
    }

    internal LocalGattDescriptor AddDescriptor(
        GattDescriptorDefinition definition,
        OnReadHandler? onRead,
        OnWriteHandler? onWrite
    )
    {
        var descriptor = new LocalGattDescriptor(definition, onRead, onWrite);
        _builder.Change(() => _descriptors.Add(descriptor));
        return descriptor;
    }

    /// <summary> Freezes the descriptors with the service; called by the builder under its lock </summary>
    internal LocalGattDescriptor[] Freeze()
    {
        LocalGattDescriptor[] descriptors = [.. _descriptors];
        Definition.Descriptors = [.. descriptors.Select(descriptor => descriptor.Definition)];
        return descriptors;
    }

    /// <summary> Sends a notification to a central </summary>
    /// <param name="central"> A central connected to the adapter that hosts the characteristic </param>
    /// <param name="value"> The value; the caller may reuse the buffer once the task completed </param>
    /// <param name="cancellationToken"> Cancels waiting to send; once the backend accepted the notification, only the wait ends </param>
    /// <returns> A task that completes once the backend accepted the notification; not when the central got it </returns>
    /// <exception cref="InvalidOperationException"> The characteristic does not notify, is not published, or the central belongs to another adapter </exception>
    /// <exception cref="BleDisconnectedException"> The central's connection ended </exception>
    public ValueTask NotifyAsync(
        ConnectedCentral central,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(central);
        BlePeripheral host = EnsureCanNotify();
        if (!ReferenceEquals(central.Peripheral, host))
            throw new InvalidOperationException("The central is connected to another adapter");
        return central.NotifyAsync(Definition, value, cancellationToken);
    }

    /// <summary> Notifies every central that turned notifications on, each independently </summary>
    /// <param name="value"> The value; the caller may reuse the buffer once the task completed </param>
    /// <param name="cancellationToken"> Cancels waiting to send; once the backend accepted a notification, only the wait ends </param>
    /// <returns> A task that completes once the backend accepted the notification for every central; a central whose
    /// notification failed, e.g. because it disconnected, is logged and skipped </returns>
    /// <exception cref="InvalidOperationException"> The characteristic does not notify or is not published </exception>
    public ValueTask NotifyAllAsync(ReadOnlyMemory<byte> value, CancellationToken cancellationToken = default) =>
        EnsureCanNotify().NotifyAllAsync(Definition, value, cancellationToken);

    private BlePeripheral EnsureCanNotify()
    {
        if (!Properties.HasFlag(GattProperty.Notify))
            throw new InvalidOperationException($"The characteristic {Uuid} does not notify");
        BlePeripheral? host;
        lock (_lock)
            host = _isLive ? _host : null;
        return host ?? throw new InvalidOperationException($"The characteristic {Uuid} is not published");
    }

    /// <summary> A publication with the characteristic starts on this peripheral </summary>
    internal void Bind(BlePeripheral host)
    {
        lock (_lock)
            _host = host;
    }

    /// <summary> The publication succeeded </summary>
    internal void MarkLive()
    {
        lock (_lock)
            _isLive = true;
    }

    /// <summary> The publication failed </summary>
    internal void Unbind()
    {
        lock (_lock)
            _host = null;
    }

    /// <summary> Answers a read of a central, through its handler </summary>
    ValueTask<byte[]> IHostedAttribute.ReadAsync(ConnectedCentral central, CancellationToken cancellationToken) =>
        ReadAsync(central, cancellationToken);

    ValueTask<GattProtocolStatus> IHostedAttribute.WriteAsync(
        ConnectedCentral central,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    ) => WriteAsync(central, value, cancellationToken);

    private ValueTask<byte[]> ReadAsync(ConnectedCentral central, CancellationToken cancellationToken) =>
        _onRead is null
            ? throw new GattException(GattProtocolStatus.ReadNotPermitted, "The characteristic has no read handler")
            : _onRead(central, cancellationToken);

    /// <summary> Handles a write of a central, through its handler </summary>
    private ValueTask<GattProtocolStatus> WriteAsync(
        ConnectedCentral central,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    ) =>
        _onWrite is null
            ? throw new GattException(GattProtocolStatus.WriteNotPermitted, "The characteristic has no write handler")
            : _onWrite(central, value, cancellationToken);
}
