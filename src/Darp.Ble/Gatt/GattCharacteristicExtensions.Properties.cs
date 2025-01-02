using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt;

public static partial class GattCharacteristicExtensions
{
    /// <summary> Get the value of the characteristic with a <see cref="Properties.Write"/> property by manually reading from it </summary>
    /// <param name="characteristic"> The characteristic to read from </param>
    /// <returns> The value </returns>
    public static byte[] GetValue(this IGattClientCharacteristic<Properties.Write> characteristic)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Characteristic.GetValue(clientPeer: null);
    }

    /// <summary> Get the value of the characteristic with a <see cref="Properties.Write"/> property by manually reading from it </summary>
    /// <param name="characteristic"> The characteristic to read from </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    /// <returns> The value </returns>
    public static T GetValue<T>(this IGattTypedClientCharacteristic<T, Properties.Write> characteristic)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Characteristic.GetValue(clientPeer: null).ToStruct<T>();
    }

    /// <summary> Update a specific value of the characteristic with a <see cref="Properties.Read"/> property by manually writing to it </summary>
    /// <param name="characteristic"> The characteristic to update </param>
    /// <param name="value"> The value to update </param>
    /// <returns> A status of the update operation </returns>
    public static GattProtocolStatus UpdateValue(this IGattClientCharacteristic<Properties.Read> characteristic, byte[] value)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Characteristic.UpdateValue(clientPeer: null, value);
    }

    /// <summary> Update a specific value of the characteristic with a <see cref="Properties.Read"/> property by manually writing to it </summary>
    /// <param name="characteristic"> The characteristic to update </param>
    /// <param name="value"> The value to update </param>
    /// <returns> A status of the update operation </returns>
    public static GattProtocolStatus UpdateValue<T>(this IGattTypedClientCharacteristic<T, Properties.Read> characteristic, T value)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        return characteristic.Characteristic.UpdateValue(clientPeer: null, value.ToByteArray());
    }

    /// <summary> Notify a connected peer of a new value </summary>
    /// <param name="characteristic"> The characteristic to be used for the notification </param>
    /// <param name="peer"> The peer to notify </param>
    /// <param name="value"> The value to update </param>
    public static void Notify(this IGattClientCharacteristic<Properties.Notify> characteristic, IGattClientPeer? peer, byte[] value)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        characteristic.Characteristic.NotifyValue(clientPeer: peer, value);
    }

    /// <summary> Notify a connected peer of a new value </summary>
    /// <param name="characteristic"> The characteristic to be used for the notification </param>
    /// <param name="peer"> The peer to notify </param>
    /// <param name="value"> The value to update </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    public static void Notify<T>(this IGattTypedClientCharacteristic<T, Properties.Notify> characteristic, IGattClientPeer peer, T value)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        characteristic.Characteristic.NotifyValue(clientPeer: peer, value.ToByteArray());
    }

    /// <summary> Notify all connected peers of a new value </summary>
    /// <param name="characteristic"> The characteristic to be used for the notification </param>
    /// <param name="value"> The value to update </param>
    public static void NotifyAll(this IGattClientCharacteristic<Properties.Notify> characteristic, byte[] value)
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        characteristic.Characteristic.NotifyValue(clientPeer: null, value);
    }

    /// <summary> Notify all connected peers of a new value </summary>
    /// <param name="characteristic"> The characteristic to be used for the notification </param>
    /// <param name="value"> The value to update </param>
    /// <typeparam name="T"> The type of the value </typeparam>
    public static void NotifyAll<T>(this IGattTypedClientCharacteristic<T, Properties.Notify> characteristic, T value)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(characteristic);
        characteristic.Characteristic.NotifyValue(clientPeer: null, value.ToByteArray());
    }
}