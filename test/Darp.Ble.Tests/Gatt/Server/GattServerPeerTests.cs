using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using FluentAssertions;
using NSubstitute;

namespace Darp.Ble.Tests.Gatt.Server;

public sealed class GattServerPeerTests
{
    [Theory]
    [InlineData]
    [InlineData(0x1234)]
    [InlineData(0x1234, 0x1235)]
    [InlineData(0x1234, 0x1235, 0x1236)]
    public async Task DiscoverServicesAsync_AnyAmountOfServices_ShouldBeDiscovered(params int[] serviceUuids)
    {
        BleUuid[] bleUuids = serviceUuids.Select(x => new BleUuid((ushort)x)).ToArray();
        IObservable<IGattServerService> observable = Observable.Create<IGattServerService>(observer =>
        {
            foreach (BleUuid bleUuid in bleUuids)
            {
                var substituteService = Substitute.For<IGattServerService>();
                substituteService.Uuid.Returns(bleUuid);
                observer.OnNext(substituteService);
            }
            observer.OnCompleted();
            return Disposable.Empty;
        });
        var serverPeer = Substitute.For<GattServerPeer>(BleAddress.NotAvailable);
        serverPeer.DiscoverServicesCore().Returns(observable);

        await serverPeer.DiscoverServicesAsync();

        serverPeer.Services.Should().HaveCount(bleUuids.Length);
        if (bleUuids.Length > 0) serverPeer.Services.Should().ContainKeys(bleUuids);
    }

    [Fact]
    public async Task DisposeAsync_ShouldCallCoreImplementation()
    {
        var device = Substitute.For<GattServerPeer>(BleAddress.NotAvailable);

        await device.DisposeAsync();

        await device.Received(1).DisposeAsyncCore();
        device.Received(1).DisposeCore();
    }
}