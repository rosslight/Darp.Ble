using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Implementation;
using Darp.Ble.Logger;
using Darp.Ble.Mock;
using FluentAssertions;
using NSubstitute;

namespace Darp.Ble.Tests.Implementation;

public sealed class BleDeviceTests
{
    [Fact]
    public async Task InitializeAsync_ShouldLog()
    {
        List<(IBleDevice, LogEvent)> resultList = [];
        BleManager manager = new BleManagerBuilder()
            .OnLog((bleDevice, logEvent) => resultList.Add((bleDevice, logEvent)))
            .With<BleMockFactory>()
            .CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync();
        resultList.Should().HaveElementAt(0, (device, new LogEvent(1, null, "Adapter Initialized!", Array.Empty<object?>())));
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task InitializeAsync_FailedInitialization_ShouldHaveCorrectResult()
    {
        var device = Substitute.For<BleDevice>((IObserver<(BleDevice, LogEvent)>?)null);
        device.InvokeNonPublicMethod("InitializeAsyncCore", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(InitializeResult.DeviceNotAvailable));

        InitializeResult result = await device.InitializeAsync();

        result.Should().Be(InitializeResult.DeviceNotAvailable);
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task InitializeAsync_SecondInitialization_AlreadyInitializing()
    {
        var device = Substitute.For<BleDevice>((IObserver<(BleDevice, LogEvent)>?)null);
        device.InvokeNonPublicMethod("InitializeAsyncCore", Arg.Any<CancellationToken>())
            .Returns(_ => Task.Delay(10).ContinueWith(_ => InitializeResult.Success));

        Task<InitializeResult> init1Task = Task.Run(async () => await device.InitializeAsync());
        Task<InitializeResult> init2Task = Task.Run(async () => await device.InitializeAsync());
        await Task.WhenAll(init1Task, init2Task);

        (await init1Task).Should().Be(InitializeResult.Success);
        (await init2Task).Should().Be(InitializeResult.AlreadyInitializing);
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task InitializeAsync_SecondInitialization_AlreadyInitialized()
    {
        var device = Substitute.For<BleDevice>((IObserver<(BleDevice, LogEvent)>?)null);
        device.InvokeNonPublicMethod("InitializeAsyncCore", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(InitializeResult.Success));

        device.IsInitialized.Should().BeFalse();
        InitializeResult init1 = await device.InitializeAsync();
        init1.Should().Be(InitializeResult.Success);

        device.IsInitialized.Should().BeTrue();
        InitializeResult init2 = await device.InitializeAsync();
        init2.Should().Be(InitializeResult.Success);
    }

    [Fact]
    public void Capability_NotInitialized_ShouldThrow()
    {
        var device = Substitute.For<BleDevice>((IObserver<(BleDevice, LogEvent)>?)null);
        Action act = () => _ = device.Observer;
        act.Should().Throw<NotInitializedException>();
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task Capability_NotSupported_ShouldThrow()
    {
        var device = Substitute.For<BleDevice>((IObserver<(BleDevice, LogEvent)>?)null);
        device.InvokeNonPublicMethod("InitializeAsyncCore", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(InitializeResult.Success));

        await device.InitializeAsync();

        Action act = () => _ = device.Observer;
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    [SuppressMessage("Usage", "NS5000:Received check.")]
    public async Task DisposeAsync()
    {
        var device = Substitute.For<BleDevice>((IObserver<(BleDevice, LogEvent)>?)null);

        await device.DisposeAsync();

        device.ReceivedWithAnyArgs(1).InvokeNonPublicMethod("DisposeAsyncCore");
        device.ReceivedWithAnyArgs(1).InvokeNonPublicMethod("DisposeCore");
    }
}