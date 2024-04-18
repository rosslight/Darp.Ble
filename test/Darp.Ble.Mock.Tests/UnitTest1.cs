namespace Darp.Ble.Mock.Tests;
/*
public static class HeartRateServiceContract
{
    public static BleUuid Uuid => new(0x1234);
    public static Characteristic<Notify> HeartRateMeasurement { get; } = new(0x2A37);
    public static Characteristic<Read<byte>> BodySensorLocation { get; } = new(0x2A38);
    public static Characteristic<Write> HeartRateControlPoint { get; } = new(0x2A39);

    public static GattServerHeartRateService AddHeartRateService(this IBlePeripheral peripheral)
    {
        var service = new GattServerHeartRateService();
        peripheral.AddService(service);
        return service;
    }

    public sealed class GattClientHeartRateService
    {
        public GattClientCharacteristic<Notify> HeartRateMeasurement { get; }
    }

    public sealed class GattServerHeartRateService : IGattServerService
    {
        public IGattServerCharacteristic<Notify> HeartRateMeasurement { get; }
        public IGattServerCharacteristic<Read<byte>> BodySensorLocation { get; }
        public IGattServerCharacteristic<Write> HeartRateControlPoint { get; }
    }
}

public static class GattProperty
{
    public sealed class Notify;
    public sealed class Write;
    public sealed class Read;
    public sealed class Read<T> where T : unmanaged;
}

public interface IGattServerCharacteristic<TProperty1>
{
    
}

public static class Ex
{
    public static IDisposable NotifyAll(this IGattServerCharacteristic<Notify> characteristic,
        IObservable<byte[]> source)
    {
        throw new NotImplementedException();
    }
    public static IDisposable UpdateReadAll<T>(this IGattServerCharacteristic<Read<T>> characteristic, T value)
        where T : unmanaged
    {
        throw new NotImplementedException();
    }
    public static IDisposable OnWrite(this IGattServerCharacteristic<Write> characteristic,
        Action<IGattClientDevice, byte[]> callback)
    {
        throw new NotImplementedException();
    }
}
*/

public sealed class UnitTest1
{
}