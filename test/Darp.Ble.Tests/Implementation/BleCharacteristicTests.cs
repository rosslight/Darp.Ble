using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Att;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Mock;
using Darp.Ble.Mock.Gatt;
using Darp.Ble.Tests.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Darp.Ble.Tests.Implementation;

public sealed class BleCharacteristicTests
{
    private static GattServerCharacteristic<TProperty> CreateCharacteristic<TProperty>(
        out IGattClientCharacteristic<TProperty> clientCharacteristic,
        out IGattClientPeer? clientPeer
    )
        where TProperty : IBleProperty
    {
        IBleDevice? mockedDevice = null;
        MockGattClientCharacteristic? createdCharacteristic = null;
        IBleDevice device = new BleMockFactory()
            .AddPeripheral(async device =>
            {
                mockedDevice = device;
                IGattClientService service = device.Peripheral.AddService(0x1234);
                createdCharacteristic = (MockGattClientCharacteristic)
                    service.AddCharacteristic(
                        TProperty.GattProperty,
                        new FuncCharacteristicValue(
                            0x1235,
                            service.Peripheral.GattDatabase,
                            _ => ValueTask.FromResult<byte[]>([0x00])
                        ),
                        []
                    );
            })
            .EnumerateDevices(NullServiceProvider.Instance)
            .First();
        device.InitializeAsync().GetAwaiter().GetResult();
        var mockClientPeer = new MockGattClientPeer(
            (MockedBlePeripheral)mockedDevice!.Peripheral,
            BleAddress.NotAvailable,
            NullLogger<MockGattClientPeer>.Instance
        );
        var characteristic = new MockGattServerCharacteristic(
            null!,
            0x1234,
            createdCharacteristic!,
            mockClientPeer,
            NullLogger<MockGattServerCharacteristic>.Instance
        );
        clientCharacteristic = new GattClientCharacteristic<TProperty>(createdCharacteristic!);
        clientPeer = mockClientPeer;
        return new GattServerCharacteristic<TProperty>(characteristic);
    }

    [Fact]
    public async Task BasicFunctionality()
    {
        byte[] bytes = Convert.FromHexString("1234");

        GattServerCharacteristic<Properties.Notify> newChar = CreateCharacteristic<Properties.Notify>(
            out IGattClientCharacteristic<Properties.Notify> clientCharacteristic,
            out IGattClientPeer? clientPeer
        );
        await using IDisposableObservable<byte[]> observable = await newChar.OnNotifyAsync();
        Task<byte[]> resultTask = observable.FirstAsync().ToTask();
        await clientCharacteristic.NotifyAsync(clientPeer, bytes);
        resultTask.Status.Should().Be(TaskStatus.RanToCompletion);
        byte[] result = await resultTask;

        result.Should().BeEquivalentTo(bytes);
    }

    [Fact]
    public async Task Unsubscribing_ShouldYieldNothing()
    {
        byte[] bytes = Convert.FromHexString("1234");

        GattServerCharacteristic<Properties.Notify> newChar = CreateCharacteristic<Properties.Notify>(
            out IGattClientCharacteristic<Properties.Notify> clientCharacteristic,
            out IGattClientPeer? clientPeer
        );
        IDisposableObservable<byte[]> observable = await newChar.OnNotifyAsync();
        Task<byte[]> resultTask = observable.FirstAsync().ToTask();
        await observable.DisposeAsync();
        resultTask.Status.Should().Be(TaskStatus.Faulted);
        await clientCharacteristic.NotifyAsync(clientPeer, bytes);
    }

    [Fact]
    public async Task SubscribingTwice_ShouldWork()
    {
        byte[] bytes = Convert.FromHexString("1234");

        GattServerCharacteristic<Properties.Notify> newChar = CreateCharacteristic<Properties.Notify>(
            out IGattClientCharacteristic<Properties.Notify> clientCharacteristic,
            out IGattClientPeer? clientPeer
        );
        IDisposableObservable<byte[]> observable1 = await newChar.OnNotifyAsync();
        IDisposableObservable<byte[]> observable2 = await newChar.OnNotifyAsync();
        Task<byte[]> resultTask1 = observable1.FirstAsync().ToTask();
        Task<byte[]> resultTask2 = observable2.FirstAsync().ToTask();
        await clientCharacteristic.NotifyAsync(clientPeer, bytes);
        byte[] result1 = await resultTask1;
        byte[] result2 = await resultTask2;
        result1.Should().BeEquivalentTo(bytes);
        result2.Should().BeEquivalentTo(bytes);
    }

    [Fact]
    public async Task UnsubscribingTwice_ShouldWork()
    {
        byte[] bytes = Convert.FromHexString("1234");

        GattServerCharacteristic<Properties.Notify> newChar = CreateCharacteristic<Properties.Notify>(
            out IGattClientCharacteristic<Properties.Notify> clientCharacteristic,
            out IGattClientPeer? clientPeer
        );
        IDisposableObservable<byte[]> observable1 = await newChar.OnNotifyAsync();
        IDisposableObservable<byte[]> observable2 = await newChar.OnNotifyAsync();
        Task<byte[]> resultTask1 = observable1.FirstAsync().ToTask();
        Task<byte[]> resultTask2 = observable2.FirstAsync().ToTask();
        await observable1.DisposeAsync();
        resultTask1.Status.Should().Be(TaskStatus.Faulted);
        resultTask2.Status.Should().Be(TaskStatus.WaitingForActivation);
        // Using return of NotifyAsync to check whether we disabled notifications
        await clientCharacteristic.NotifyAsync(clientPeer, bytes);
        await observable2.DisposeAsync();
        resultTask2.Status.Should().Be(TaskStatus.RanToCompletion);
        await clientCharacteristic.NotifyAsync(clientPeer, bytes);
    }

    [Fact]
    public async Task ReSubscription_ShouldWaitOnUnsubscription()
    {
        byte[] bytes = Convert.FromHexString("1234");

        GattServerCharacteristic<Properties.Notify> newChar = CreateCharacteristic<Properties.Notify>(
            out IGattClientCharacteristic<Properties.Notify> clientCharacteristic,
            out IGattClientPeer? clientPeer
        );
        IDisposableObservable<byte[]> notifyObservable = await newChar.OnNotifyAsync();
        Task<byte[]> resultTask = notifyObservable.FirstAsync().ToTask();
        await clientCharacteristic.NotifyAsync(clientPeer, bytes);
        resultTask.Status.Should().Be(TaskStatus.RanToCompletion);
        await notifyObservable.DisposeAsync();
        IDisposableObservable<byte[]> notifyObservable2 = await newChar.OnNotifyAsync();
        Task<byte[]> resultTask2 = notifyObservable2.FirstAsync().ToTask();
        await clientCharacteristic.NotifyAsync(clientPeer, bytes);
        resultTask2.Status.Should().Be(TaskStatus.RanToCompletion);
        await notifyObservable2.DisposeAsync();
    }
}
