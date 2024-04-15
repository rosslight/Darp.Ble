using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock.Gatt;

public sealed class MockGattClientCharacteristic(GattProperty property) : IPlatformSpecificGattClientCharacteristic
{
    public GattProperty Property { get; } = property;
    public IDisposable OnWrite(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback)
    {
        throw new NotImplementedException();
    }

    public Task<bool> NotifyAsync(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}