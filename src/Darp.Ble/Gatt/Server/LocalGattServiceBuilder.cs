using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> Describes a service to add with <see cref="Darp.Ble.Connections.BlePeripheral.AddServiceAsync"/>. A builder is used once and not shared between threads </summary>
public sealed class LocalGattServiceBuilder
{
    private readonly BleUuid _uuid;
    private readonly bool _isPrimary;
    private readonly List<GattCharacteristicDefinition> _characteristics = [];
    private bool _added;

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
    /// <exception cref="InvalidOperationException"> The service was already added </exception>
    public LocalGattCharacteristic AddCharacteristic(BleUuid uuid, GattProperty properties)
    {
        ArgumentNullException.ThrowIfNull(uuid);
        var definition = new GattCharacteristicDefinition(uuid, properties);
        EnsureNotAdded();
        _characteristics.Add(definition);
        return new LocalGattCharacteristic(definition);
    }

    /// <summary> Freezes the service and builds its definition; the builder cannot change or be added again afterwards </summary>
    internal GattServiceDefinition Build()
    {
        EnsureNotAdded();
        _added = true;
        return new GattServiceDefinition(_uuid, _isPrimary, [.. _characteristics]);
    }

    private void EnsureNotAdded()
    {
        if (_added)
            throw new InvalidOperationException(
                "The service was already added with BlePeripheral.AddServiceAsync and can no longer change; use a new LocalGattServiceBuilder to add another service"
            );
    }
}
