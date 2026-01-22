using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Implementation;
using Darp.Ble.Tests.TestUtils;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

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
        BleUuid[] bleUuids = serviceUuids.Select(i => BleUuid.FromUInt16((ushort)i)).ToArray();
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
        var serverPeer = Substitute.For<GattServerPeer>(
            null!,
            BleAddress.NotAvailable,
            NullLogger<GattServerPeer>.Instance
        );
        serverPeer.DiscoverServicesCore().Returns(observable);

        await serverPeer.DiscoverServicesAsync();

        serverPeer.Services.Count.ShouldBe(bleUuids.Length);
        if (bleUuids.Length > 0)
        {
            serverPeer.Services.Select(x => x.Uuid).ShouldBe(bleUuids);
        }
    }

    [Fact]
    public async Task DisposeAsync_ShouldCallCoreImplementation()
    {
        var central = Substitute.For<BleCentral>(null!, NullLogger<BleCentral>.Instance);
        var device = Substitute.For<GattServerPeer>(
            central,
            BleAddress.NotAvailable,
            NullLogger<GattServerPeer>.Instance
        );
#pragma warning disable CA2012 // Value task should be awaited
        device
            .DisposeAsyncCore()
            .Returns(_ =>
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
