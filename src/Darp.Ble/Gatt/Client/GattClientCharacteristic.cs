using System.Reactive.Subjects;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

public sealed class GattClientCharacteristic(BleUuid uuid, IGattClientService service)
{
    private readonly Subject<byte[]> _onWrite = new();
    private readonly IGattClientService _service = service;

    public BleUuid Uuid { get; } = uuid;

    public IDisposable OnWrite(Action<IGattClientPeer, byte[]> callback)
    {
        return _onWrite.Subscribe(bytes => callback(_service, bytes));
    }

    public void Notify(IGattClientPeer clientPeer, byte[] source)
    {
        throw new NotImplementedException();
    }
}

public sealed class GattClientCharacteristic<TProp1>(Characteristic<TProp1> characteristic, IGattClientService service)
    : IGattClientCharacteristic<TProp1>
{
    public GattClientCharacteristic Characteristic { get; } = new(characteristic.Uuid, service);
}