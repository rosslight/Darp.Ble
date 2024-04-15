using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Gatt.Client;

public static class GattClientCharacteristicExtensions
{
    public static async Task<IGattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(this GattClientService service,
        Characteristic<TProp1> characteristic,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        return await service.AddCharacteristicAsync<TProp1>(characteristic.Uuid, characteristic.Property, cancellationToken);
    }

    public static async Task<IGattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(this GattClientService service,
        BleUuid uuid,
        GattProperty property,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        GattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(uuid, property, cancellationToken);
        return new GattClientCharacteristic<TProp1>(clientCharacteristic);
    }

    public static async Task NotifyAsync(this IGattClientCharacteristic<Property.Notify> characteristic,
        IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken = default)
    {
        await characteristic.Characteristic.NotifyAsync(clientPeer, source, cancellationToken);
    }

    public static void NotifyAll(this IGattClientCharacteristic<Property.Notify> characteristic,
        IObservable<byte[]> source)
    {
        throw new NotImplementedException();
    }

    public static IDisposable UpdateReadAll<T>(this IGattClientCharacteristic<Property.Read<T>> characteristic, T value)
        where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public static IDisposable OnWrite(this IGattClientCharacteristic<Property.Write> characteristic,
        Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback)
    {
        return characteristic.Characteristic.OnWrite(callback);
    }

    public static IDisposable OnWrite(this IGattClientCharacteristic<Property.Write> characteristic,
        Func<IGattClientPeer, byte[], GattProtocolStatus> callback)
    {
        return characteristic.Characteristic.OnWrite((peer, bytes, _) => Task.FromResult(callback(peer, bytes)));
    }
}