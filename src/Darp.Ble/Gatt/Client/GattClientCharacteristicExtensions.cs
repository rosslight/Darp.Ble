using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

public static class GattClientCharacteristicExtensions
{
    public static async Task<IGattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(this IGattClientService service,
        Characteristic<TProp1> characteristic,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(characteristic.Uuid, characteristic.Property, cancellationToken);
        return new GattClientCharacteristic<TProp1>(clientCharacteristic);
    }

    public static async Task<IGattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(this IGattClientService service,
        BleUuid uuid,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        IGattClientCharacteristic clientCharacteristic = await service.AddCharacteristicAsync(uuid, TProp1.GattProperty, cancellationToken);
        return new GattClientCharacteristic<TProp1>(clientCharacteristic);
    }

    public static async Task<IGattClientCharacteristic<TProp1>> AddCharacteristicAsync<TProp1>(this IGattClientService service,
        ushort uuid,
        CancellationToken cancellationToken = default)
        where TProp1 : IBleProperty
    {
        return await service.AddCharacteristicAsync<TProp1>(new BleUuid(uuid), cancellationToken);
    }

    public static async Task NotifyAsync(this IGattClientCharacteristic<Properties.Notify> characteristic,
        IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken = default)
    {
        await characteristic.Characteristic.NotifyAsync(clientPeer, source, cancellationToken);
    }

    public static void NotifyAll(this IGattClientCharacteristic<Properties.Notify> characteristic,
        IObservable<byte[]> source)
    {
        throw new NotImplementedException();
    }

    public static IDisposable UpdateReadAll<T>(this IGattClientCharacteristic<Properties.Read<T>> characteristic, T value)
        where T : unmanaged
    {
        throw new NotImplementedException();
    }
    
    public static IDisposable OnWrite(this IGattClientCharacteristic<Properties.Write> characteristic,
        Func<IGattClientPeer, byte[], Task<GattProtocolStatus>> callback)
    {
        return characteristic.Characteristic.OnWrite((peer, bytes, _) => callback(peer, bytes));
    }

    public static IDisposable OnWrite(this IGattClientCharacteristic<Properties.Write> characteristic,
        Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback)
    {
        return characteristic.Characteristic.OnWrite(callback);
    }

    public static IDisposable OnWrite(this IGattClientCharacteristic<Properties.Write> characteristic,
        Func<IGattClientPeer, byte[], GattProtocolStatus> callback)
    {
        return characteristic.Characteristic.OnWrite((peer, bytes, _) => Task.FromResult(callback(peer, bytes)));
    }
}