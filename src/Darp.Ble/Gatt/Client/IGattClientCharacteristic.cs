using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> A gatt client characteristic </summary>
public interface IGattClientCharacteristic : IGattClientAttribute
{
    /// <summary> The service this characteristic was added to </summary>
    IGattClientService Service { get; }

    /// <summary> The handle of the characteristic attribute </summary>
    ushort AttributeHandle { get; }
    /// <summary> The UUID of the characteristic </summary>
    BleUuid Uuid { get; }
    /// <summary> The property of the characteristic </summary>
    GattProperty Properties { get; }
    /// <summary> The descriptors added to this characteristic </summary>
    IReadOnlyDictionary<BleUuid, IGattClientDescriptor> Descriptors { get; }

    /// <summary> Add a new descriptor </summary>
    /// <param name="uuid"> The uuid of the descriptor to be added </param>
    /// <param name="onRead"> The callback to be called when a read operation was requested on this attribute </param>
    /// <param name="onWrite"> The callback to be called when a write operation was requested on this attribute </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task which holds the descriptor on completion </returns>
    Task<IGattClientDescriptor> AddDescriptorAsync(BleUuid uuid,
        OnReadCallback? onRead = null,
        OnWriteCallback? onWrite = null,
        CancellationToken cancellationToken = default);

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
