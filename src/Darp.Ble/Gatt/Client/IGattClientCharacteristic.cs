using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> A gatt client characteristic </summary>
public interface IGattClientCharacteristic
{
    /// <summary> The UUID of the characteristic </summary>
    BleUuid Uuid { get; }
    /// <summary> The property of the characteristic </summary>
#pragma warning disable CA1716 // Using a reserved keyword as the name of a virtual/ interface member makes it harder for consumers in other languages to override/ implement the member.
    GattProperty Property { get; }
#pragma warning restore CA1716

    /// <summary> Get the current value of the characteristic </summary>
    /// <param name="clientPeer"> The client peer to get the value for. If null, all subscribed clients will be taken into account </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> The current value </returns>
    ValueTask<byte[]> GetValueAsync(IGattClientPeer? clientPeer, CancellationToken cancellationToken);
    /// <summary> Update the characteristic value </summary>
    /// <param name="clientPeer"> The client peer to update the value for. If null, all subscribed clients will be taken into account </param>
    /// <param name="value"> The value to update with </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> The status of the update operation </returns>
    ValueTask<GattProtocolStatus> UpdateValueAsync(IGattClientPeer? clientPeer, byte[] value, CancellationToken cancellationToken);
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
/// <typeparam name="TProperty1"> The type of the property </typeparam>
public interface IGattClientCharacteristic<TProperty1>
    where TProperty1 : IBleProperty
{
    /// <inheritdoc cref="IGattClientCharacteristic.Uuid"/>
    BleUuid Uuid => Characteristic.Uuid;
    /// <inheritdoc cref="IGattClientCharacteristic.Property"/>
#pragma warning disable CA1716 // Using a reserved keyword as the name of a virtual/ interface member makes it harder for consumers in other languages to override/ implement the member.
    GattProperty Property => Characteristic.Property;
#pragma warning restore CA1716
    /// <summary> The gatt client characteristic </summary>
    IGattClientCharacteristic Characteristic { get; }
}

/// <summary> A gatt client characteristic with a single proprety and a specified type for the value </summary>
/// <typeparam name="T"> The type of the value </typeparam>
/// <typeparam name="TProperty1"> The type of the property </typeparam>
public interface IGattTypedClientCharacteristic<T, TProperty1> : IGattClientCharacteristic<TProperty1>
    where TProperty1 : IBleProperty
{
    /// <summary> Read the value from a given source of bytes </summary>
    /// <param name="source"> The source to read from </param>
    /// <returns> The value </returns>
    T ReadValue(ReadOnlySpan<byte> source);
    /// <summary> Write a specific value into a destination of bytes </summary>
    /// <param name="value"> The value to write </param>
    /// <returns> The byte array </returns>
    byte[] WriteValue(T value);
}