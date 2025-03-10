using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;

namespace Darp.Ble.Gatt.Client;

public interface IGattCharacteristicDeclaration : IGattAttribute
{
    /// <summary> The property of the characteristic </summary>
    GattProperty Properties { get; }
}

public interface IGattCharacteristicValue : IGattAttribute;

/// <summary> A gatt client characteristic </summary>
public interface IGattClientCharacteristic
{
    /// <summary> The service this characteristic was added to </summary>
    IGattClientService Service { get; }

    /// <summary> The UUID of the characteristic </summary>
    BleUuid Uuid { get; }

    /// <summary> The property of the characteristic </summary>
    GattProperty Properties { get; }

    /// <summary> The descriptors added to this characteristic </summary>
    internal IReadonlyAttributeCollection<IGattCharacteristicValue> Descriptors { get; }

    /// <summary> Access the characteristic declaration </summary>
    internal IGattCharacteristicDeclaration Declaration { get; }

    /// <summary> Access the characteristic value </summary>
    internal IGattCharacteristicValue Value { get; }

    /// <summary> Add a new descriptor </summary>
    /// <param name="value"> The characteristic value </param>
    void AddDescriptor(IGattCharacteristicValue value);

    /// <summary> Notify subscribers about a new value </summary>
    /// <param name="clientPeer"> The client peer to notify. If null, all subscribed clients will be taken into account </param>
    /// <param name="value"> The value to update with </param>
    void NotifyValue(IGattClientPeer? clientPeer, byte[] value);

    /// <summary> Update the characteristic value </summary>
    /// <param name="clientPeer"> The client peer to update the value for. If null, all subscribed clients will be taken into account </param>
    /// <param name="value"> The value to update with </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> A task completing when indication was acknowledged </returns>
    Task IndicateAsync(IGattClientPeer? clientPeer, byte[] value, CancellationToken cancellationToken);
}

/// <summary> A gatt client characteristic with a single property </summary>
/// <typeparam name="TProp1"> The type of the property </typeparam>
public interface IGattClientCharacteristic<TProp1> : IGattClientCharacteristic
    where TProp1 : IBleProperty;
