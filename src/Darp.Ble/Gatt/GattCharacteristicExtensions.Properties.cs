using System.Reactive;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using static Darp.Ble.Gatt.Properties;

namespace Darp.Ble.Gatt;

public static partial class GattCharacteristicExtensions
{
    /// <summary> Get the value of the characteristic with a <see cref="Properties.Write"/> property by manually reading from it </summary>
    /// <param name="characteristic"> The characteristic to read from </param>
    /// <returns> The value </returns>
    public static ValueTask<byte[]> GetValueAsync(this IGattClientCharacteristic<Write> characteristic)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Value.ReadValueAsync(clientPeer: null, characteristic.ServiceProvider);
    }

    /// <summary> Get the value of the characteristic with a <see cref="Properties.Write"/> property by manually reading from it </summary>
    /// <param name="characteristic"> The characteristic to read from </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <returns> The value </returns>
    public static async ValueTask<T> GetValueAsync<T>(this IGattTypedClientCharacteristic<T, Write> characteristic)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        byte[] bytes = await characteristic
            .Value.ReadValueAsync(clientPeer: null, characteristic.ServiceProvider)
            .ConfigureAwait(false);
        return characteristic.Decode(bytes);
    }

    /// <summary> Update a specific value of the characteristic with a <see cref="Properties.Read"/> property by manually writing to it </summary>
    /// <param name="characteristic"> The characteristic to update </param>
    /// <param name="value"> The value to update </param>
    /// <returns> A status of the update operation </returns>
    public static ValueTask<GattProtocolStatus> UpdateValueAsync(
        this IGattClientCharacteristic<Read> characteristic,
        byte[] value
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Value.WriteValueAsync(clientPeer: null, value, characteristic.ServiceProvider);
    }

    /// <summary> Update a specific value of the characteristic with a <see cref="Properties.Read"/> property by manually writing to it </summary>
    /// <param name="characteristic"> The characteristic to update </param>
    /// <param name="value"> The value to update </param>
    /// <returns> A status of the update operation </returns>
    public static ValueTask<GattProtocolStatus> UpdateValueAsync<T>(
        this IGattTypedClientCharacteristic<T, Read> characteristic,
        T value
    )
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Value.WriteValueAsync(
            clientPeer: null,
            characteristic.Encode(value),
            characteristic.ServiceProvider
        );
    }

    /// <summary> Notify a connected peer of a new value </summary>
    /// <param name="characteristic"> The characteristic to be used for the notification </param>
    /// <param name="clientPeer"> The client peer to notify. If null, all subscribed clients will be taken into account </param>
    /// <param name="value"> The value to update </param>
    public static async ValueTask<Unit> NotifyAsync(
        this IGattClientCharacteristic<Notify> characteristic,
        IGattClientPeer? clientPeer,
        byte[] value
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        await characteristic.NotifyValueAsync(clientPeer: clientPeer, value: value).ConfigureAwait(false);
        return Unit.Default;
    }

    /// <summary> Notify a connected peer of a new value </summary>
    /// <param name="characteristic"> The characteristic to be used for the notification </param>
    /// <param name="clientPeer"> The client peer to notify. If null, all subscribed clients will be taken into account </param>
    /// <param name="value"> The value to update </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    public static async ValueTask<Unit> NotifyAsync<T>(
        this IGattTypedClientCharacteristic<T, Notify> characteristic,
        IGattClientPeer? clientPeer,
        T value
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        await characteristic
            .NotifyValueAsync(clientPeer: clientPeer, value: characteristic.Encode(value))
            .ConfigureAwait(false);
        return Unit.Default;
    }

    /// <summary> Notify all connected peers of a new value </summary>
    /// <param name="characteristic"> The characteristic to be used for the notification </param>
    /// <param name="value"> The value to update </param>
    public static ValueTask<Unit> NotifyAllAsync(this IGattClientCharacteristic<Notify> characteristic, byte[] value)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.NotifyAsync(clientPeer: null, value: value);
    }

    /// <summary> Notify all connected peers of a new value </summary>
    /// <param name="characteristic"> The characteristic to be used for the notification </param>
    /// <param name="value"> The value to update </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    public static ValueTask<Unit> NotifyAllAsync<T>(
        this IGattTypedClientCharacteristic<T, Notify> characteristic,
        T value
    )
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.NotifyAsync(clientPeer: null, value: value);
    }
}
