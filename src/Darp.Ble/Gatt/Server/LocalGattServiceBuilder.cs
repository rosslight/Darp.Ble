using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> Describes a service to publish with <see cref="GattServer.AddServiceAsync"/>. A builder publishes one service once </summary>
public sealed class LocalGattServiceBuilder
{
    private readonly BleUuid _uuid;

    internal BleUuid Uuid => _uuid;
    private readonly bool _isPrimary;
    private readonly Lock _lock = new();
    private readonly List<LocalGattCharacteristic> _characteristics = [];
    private bool _published;

    /// <summary> Starts a service </summary>
    /// <param name="uuid"> The UUID of the service </param>
    /// <param name="isPrimary"> Whether the service is primary; otherwise it is secondary </param>
    public LocalGattServiceBuilder(BleUuid uuid, bool isPrimary = true)
    {
        ArgumentNullException.ThrowIfNull(uuid);
        _uuid = uuid;
        _isPrimary = isPrimary;
    }

    /// <summary> Starts the service a declaration describes </summary>
    /// <param name="declaration"> The declaration of the service </param>
    public LocalGattServiceBuilder(ServiceDeclaration declaration)
        : this(
            (declaration ?? throw new ArgumentNullException(nameof(declaration))).Uuid,
            declaration.Type is not GattServiceType.Secondary
        ) { }

    /// <summary> Adds a characteristic </summary>
    /// <param name="uuid"> The UUID of the characteristic </param>
    /// <param name="properties"> What the characteristic supports </param>
    /// <param name="onRead"> Answers reads of centrals; without it a read is answered with <see cref="GattProtocolStatus.ReadNotPermitted"/> </param>
    /// <param name="onWrite"> Handles write requests of centrals; without it a write is answered with <see cref="GattProtocolStatus.WriteNotPermitted"/> </param>
    /// <returns> The characteristic </returns>
    /// <exception cref="InvalidOperationException"> The service was already published </exception>
    public LocalGattCharacteristic AddCharacteristic(
        BleUuid uuid,
        GattProperty properties,
        OnReadHandler? onRead = null,
        OnWriteHandler? onWrite = null
    )
    {
        ArgumentNullException.ThrowIfNull(uuid);
        var characteristic = new LocalGattCharacteristic(
            this,
            new GattCharacteristicDefinition(uuid, properties),
            onRead,
            onWrite
        );
        lock (_lock)
        {
            EnsureNotPublished();
            _characteristics.Add(characteristic);
        }
        return characteristic;
    }

    /// <summary> Freezes the service for publishing; the builder cannot change or publish again afterwards </summary>
    internal (GattServiceDefinition Definition, IHostedAttribute[] Attributes) Publish()
    {
        lock (_lock)
        {
            EnsureNotPublished();
            _published = true;
            LocalGattCharacteristic[] characteristics = [.. _characteristics];
            List<IHostedAttribute> attributes = [];
            foreach (LocalGattCharacteristic characteristic in characteristics)
            {
                attributes.Add(characteristic);
                attributes.AddRange(characteristic.Freeze());
            }
            var definition = new GattServiceDefinition(
                _uuid,
                _isPrimary,
                [.. characteristics.Select(characteristic => characteristic.Definition)]
            );
            return (definition, [.. attributes]);
        }
    }

    /// <summary> Changes the service while it is not published </summary>
    /// <exception cref="InvalidOperationException"> The service was already published </exception>
    internal void Change(Action change)
    {
        lock (_lock)
        {
            EnsureNotPublished();
            change();
        }
    }

    private void EnsureNotPublished()
    {
        if (_published)
            throw new InvalidOperationException("The service was already published; start a new builder");
    }
}
