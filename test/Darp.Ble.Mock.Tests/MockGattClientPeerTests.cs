using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Mock;
using Darp.Ble.Mock.Gatt;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Tests;

public sealed class MockGattClientPeerTests(ILoggerFactory loggerFactory)
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    private async Task<(
        MockGattClientPeer ClientPeer,
        MockedBlePeripheral Peripheral,
        IGattServerPeer ServerPeer
    )> CreateConnectedPeerAsync()
    {
        var address = BleAddress.NewRandomStaticAddress();
        BleManager manager = new BleManagerBuilder()
            .AddMock(factory =>
                factory.AddPeripheral(async device =>
                {
                    await device.SetRandomAddressAsync(address).ConfigureAwait(false);
                })
            )
            .SetLogger(_loggerFactory)
            .CreateManager();
        var device = (MockBleDevice)manager.EnumerateDevices().First();
        await device.InitializeAsync();

        MockedBlePeripheral peripheral = device.MockedDevices.First().Peripheral;
        IGattServerPeer serverPeer = await device.Central.ConnectToPeripheral(address).FirstAsync();
        var clientPeer = (MockGattClientPeer)peripheral.PeerDevices[address];

        return (clientPeer, peripheral, serverPeer);
    }

    [Fact]
    public async Task WhenDisconnected_ShouldEmit_WhenServerPeerDisposed()
    {
        (MockGattClientPeer clientPeer, _, IGattServerPeer serverPeer) = await CreateConnectedPeerAsync();

        bool disconnectedReceived = false;
        using IDisposable subscription = clientPeer.WhenDisconnected.Subscribe(_ => disconnectedReceived = true);

        await serverPeer.DisposeAsync();
        await Task.Delay(10);

        disconnectedReceived.Should().BeTrue("client peer should emit WhenDisconnected on disposal");
        clientPeer.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Peripheral_WhenDisconnected_ShouldEmit_WhenServerPeerDisposed()
    {
        (MockGattClientPeer clientPeer, MockedBlePeripheral peripheral, IGattServerPeer serverPeer) =
            await CreateConnectedPeerAsync();

        Task peripheralDisconnectedTask = peripheral.WhenDisconnected.Take(1).ToTask();

        await serverPeer.DisposeAsync();
        Task completedTask = await Task.WhenAny(peripheralDisconnectedTask, Task.Delay(100));

        completedTask.Should().BeSameAs(peripheralDisconnectedTask);
        clientPeer.IsConnected.Should().BeFalse();
    }
}
