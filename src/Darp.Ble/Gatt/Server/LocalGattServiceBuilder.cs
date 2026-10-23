using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> Describes a service to publish with <see cref="GattServer.AddServiceAsync"/>. A builder publishes one service once </summary>
public sealed class LocalGattServiceBuilder
{
    private readonly BleUuid _uuid;
    private readonly bool _isPrimary;
    private readonly Lock _lock = new();
    private readonly List<GattCharacteristicDefinition> _characteristics = [];
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
    /// <returns> The characteristic </returns>
    /// <exception cref="InvalidOperationException"> The service was already published </exception>
    public LocalGattCharacteristic AddCharacteristic(BleUuid uuid, GattProperty properties)
    {
        ArgumentNullException.ThrowIfNull(uuid);
        var definition = new GattCharacteristicDefinition(uuid, properties);
        lock (_lock)
        {
            EnsureNotPublished();
            _characteristics.Add(definition);
        }
        return new LocalGattCharacteristic(definition);
    }

    /// <summary> Freezes the service for publishing; the builder cannot change or publish again afterwards </summary>
    internal GattServiceDefinition Publish()
    {
        lock (_lock)
        {
            EnsureNotPublished();
            _published = true;
            return new GattServiceDefinition(_uuid, _isPrimary, [.. _characteristics]);
        }
    }

    private void EnsureNotPublished()
    {
        if (_published)
            throw new InvalidOperationException("The service was already published; start a new builder");
    }
}
