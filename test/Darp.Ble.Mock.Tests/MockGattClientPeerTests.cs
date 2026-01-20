using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
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

    [Fact(Timeout = 5000)]
    public async Task WhenDisconnected_ShouldEmit_WhenServerPeerDisposed()
    {
        (MockGattClientPeer clientPeer, _, IGattServerPeer serverPeer) = await CreateConnectedPeerAsync();

        Task<Unit> peripheralDisconnectedTask = clientPeer.WhenDisconnected.Take(1).ToTask();

        await serverPeer.DisposeAsync();

        await peripheralDisconnectedTask;

        clientPeer.IsConnected.Should().BeFalse();
    }

    [Fact(Timeout = 5000)]
    public async Task Peripheral_WhenDisconnected_ShouldEmit_WhenServerPeerDisposed()
    {
        (MockGattClientPeer clientPeer, MockedBlePeripheral peripheral, IGattServerPeer serverPeer) =
            await CreateConnectedPeerAsync();

        Task<IGattClientPeer> peripheralDisconnectedTask = peripheral.WhenDisconnected.Take(1).ToTask();

        await serverPeer.DisposeAsync();
        IGattClientPeer completedPeer = await peripheralDisconnectedTask;

        completedPeer.Should().BeSameAs(clientPeer);
        clientPeer.IsConnected.Should().BeFalse();
    }

    [Fact(Timeout = 5000)]
    public async Task DisposeDevice_WithConnectedCentral_ShouldNotThrowObjectDisposedException()
    {
        (_, _, IGattServerPeer serverPeer) = await CreateConnectedPeerAsync();
        var device = (MockBleDevice)serverPeer.Central.Device;

        // Disposing the device should dispose Central first, which disposes server peers.
        // This triggers client peer disconnect, which fires the WhenDisconnected observable.
        // The subscription in BlePeripheral.OnConnectedCentral tries to call OnNext on
        // _whenDisconnected, but if Peripheral is disposed before the callback completes,
        // it will throw ObjectDisposedException.
        Func<Task> act = async () => await device.DisposeAsync();

        await act.Should().NotThrowAsync<ObjectDisposedException>();
    }
}
