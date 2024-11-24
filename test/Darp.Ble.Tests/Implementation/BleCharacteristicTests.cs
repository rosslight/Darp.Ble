using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Mock.Gatt;
using FluentAssertions;

namespace Darp.Ble.Tests.Implementation;

public sealed class BleCharacteristicTests
{
    private static GattServerCharacteristic<TProperty> CreateCharacteristic<TProperty>(
        out IGattClientCharacteristic clientCharacteristic,
        out IGattClientPeer clientPeer)
        where TProperty : IBleProperty
    {
        var characteristicUuid = new BleUuid(0x1234);
        var mockClientPeer = MockGattClientPeer.TestClientPeer;
        var mockClientChar = new MockGattClientCharacteristic(new BleUuid(0x1234), TProperty.GattProperty);
        var characteristic = new MockGattServerCharacteristic(characteristicUuid, mockClientChar, mockClientPeer);
        clientCharacteristic = mockClientChar;
        clientPeer = mockClientPeer;
        return new GattServerCharacteristic<TProperty>(characteristic);
    }

    [Fact]
    public async Task Test()
    {
        var bytes = Convert.FromHexString("1234");

        GattServerCharacteristic<Properties.Notify> newChar = CreateCharacteristic<Properties.Notify>(
            out IGattClientCharacteristic clientCharacteristic,
            out IGattClientPeer clientPeer);
        var observable = await newChar.OnNotifyAsync();
        var resultTask = observable.FirstAsync().ToTask();
        await clientCharacteristic.NotifyAsync(clientPeer, bytes, default);
        var result = await resultTask;

        result.Should().BeEquivalentTo(bytes);
    }
}