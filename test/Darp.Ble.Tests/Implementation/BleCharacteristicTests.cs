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
    public async Task BasicFunctionality()
    {
        byte[] bytes = Convert.FromHexString("1234");

        GattServerCharacteristic<Properties.Notify> newChar = CreateCharacteristic<Properties.Notify>(
            out IGattClientCharacteristic clientCharacteristic,
            out IGattClientPeer clientPeer);
        await using IDisposableObservable<byte[]> observable = await newChar.OnNotifyAsync();
        Task<byte[]> resultTask = observable.FirstAsync().ToTask();
        await clientCharacteristic.NotifyAsync(clientPeer, bytes, default);
        resultTask.Status.Should().Be(TaskStatus.RanToCompletion);
        byte[] result = await resultTask;

        result.Should().BeEquivalentTo(bytes);
    }

    [Fact]
    public async Task Unsubscribing_ShouldYieldNothing()
    {
        byte[] bytes = Convert.FromHexString("1234");

        GattServerCharacteristic<Properties.Notify> newChar = CreateCharacteristic<Properties.Notify>(
            out IGattClientCharacteristic clientCharacteristic,
            out IGattClientPeer clientPeer);
        IDisposableObservable<byte[]> observable = await newChar.OnNotifyAsync();
        Task<byte[]> resultTask = observable.FirstAsync().ToTask();
        await observable.DisposeAsync();
        resultTask.Status.Should().Be(TaskStatus.Faulted);
        var result = await clientCharacteristic.NotifyAsync(clientPeer, bytes, default);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SubscribingTwice_ShouldWork()
    {
        byte[] bytes = Convert.FromHexString("1234");

        GattServerCharacteristic<Properties.Notify> newChar = CreateCharacteristic<Properties.Notify>(
            out IGattClientCharacteristic clientCharacteristic,
            out IGattClientPeer clientPeer);
        IDisposableObservable<byte[]> observable1 = await newChar.OnNotifyAsync();
        IDisposableObservable<byte[]> observable2 = await newChar.OnNotifyAsync();
        Task<byte[]> resultTask1 = observable1.FirstAsync().ToTask();
        Task<byte[]> resultTask2 = observable2.FirstAsync().ToTask();
        bool result = await clientCharacteristic.NotifyAsync(clientPeer, bytes, default);
        result.Should().BeTrue();
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
            out IGattClientCharacteristic clientCharacteristic,
            out IGattClientPeer clientPeer);
        IDisposableObservable<byte[]> observable1 = await newChar.OnNotifyAsync();
        IDisposableObservable<byte[]> observable2 = await newChar.OnNotifyAsync();
        Task<byte[]> resultTask1 = observable1.FirstAsync().ToTask();
        Task<byte[]> resultTask2 = observable2.FirstAsync().ToTask();
        await observable1.DisposeAsync();
        resultTask1.Status.Should().Be(TaskStatus.Faulted);
        resultTask2.Status.Should().Be(TaskStatus.WaitingForActivation);
        // Using return of NotifyAsync to check whether we disabled notifications
        var result1 = await clientCharacteristic.NotifyAsync(clientPeer, bytes, default);
        result1.Should().BeTrue();
        await observable2.DisposeAsync();
        resultTask2.Status.Should().Be(TaskStatus.RanToCompletion);
        var result2 = await clientCharacteristic.NotifyAsync(clientPeer, bytes, default);
        result2.Should().BeFalse();
    }
}