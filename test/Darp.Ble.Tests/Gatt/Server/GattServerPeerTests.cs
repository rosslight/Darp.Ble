using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.Tests.TestUtils;
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
    public async Task DiscoverServicesAsync_AnyAmountOfServices_ShouldBeDiscovered(params ushort[] serviceUuids)
    {
        BleUuid[] bleUuids = serviceUuids.Select(BleUuid.FromUInt16).ToArray();
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
        var serverPeer = Substitute.For<GattServerPeer>(null!, BleAddress.NotAvailable, null);
        serverPeer.DiscoverServicesCore().Returns(observable);

        await serverPeer.DiscoverServicesAsync();

        serverPeer.Services.Should().HaveCount(bleUuids.Length);
        if (bleUuids.Length > 0)
        {
            serverPeer.Services
                .Select(x => x.Uuid)
                .Should().BeEquivalentTo(bleUuids);
        }
    }

    [Fact]
    public async Task DisposeAsync_ShouldCallCoreImplementation()
    {
        var central = Substitute.For<BleCentral>(null!, null);
        var device = Substitute.For<GattServerPeer>(central, BleAddress.NotAvailable, null);
#pragma warning disable CA2012 // Value task should be awaited
        device.DisposeAsyncCore().Returns(_ =>
#pragma warning restore CA2012
        {
            var subject = device.GetNonPublicProperty<BehaviorSubject<ConnectionStatus>>("ConnectionSubject");
            subject.OnNext(ConnectionStatus.Disconnected);
            return ValueTask.CompletedTask;
        });
        await device.DisposeAsync();

        await device.Received(1).DisposeAsyncCore();
        device.Received(1).DisposeCore();
    }
}