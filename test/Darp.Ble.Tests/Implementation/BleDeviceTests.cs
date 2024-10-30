using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Implementation;
using Darp.Ble.Mock;
using Darp.Ble.Tests.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Reactive.Testing;
using NSubstitute;

namespace Darp.Ble.Tests.Implementation;

public sealed class BleDeviceTests
{
    [Fact]
    public async Task InitializeAsync_ShouldLog()
    {
        var logger = new TestLogger();
        BleManager manager = new BleManagerBuilder()
            .SetLogger(logger)
            .Add<BleMockFactory>()
            .CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync();
        logger.LogEntries.Should().BeEquivalentTo([(LogLevel.Debug, $"Ble device '{device.Name}' initialized!")]);
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task InitializeAsync_FailedInitialization_ShouldHaveCorrectResult()
    {
        var device = Substitute.For<BleDevice>((ILogger?)null);
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
        var testScheduler = new TestScheduler();

        var device = Substitute.For<BleDevice>((ILogger?)null);
        device.InvokeNonPublicMethod("InitializeAsyncCore", Arg.Any<CancellationToken>())
            .Returns(_ => Observable.Return(InitializeResult.Success)
                .Delay(TimeSpan.FromMilliseconds(1000), testScheduler)
                .ToTask());

        Task<InitializeResult> init1Task = device.InitializeAsync();
        Task<InitializeResult> init2Task = device.InitializeAsync();

        testScheduler.AdvanceTo(TimeSpan.FromMilliseconds(1001).Ticks);

        (await init1Task).Should().Be(InitializeResult.Success);
        (await init2Task).Should().Be(InitializeResult.AlreadyInitializing);
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task InitializeAsync_SecondInitialization_AlreadyInitialized()
    {
        var device = Substitute.For<BleDevice>((ILogger?)null);
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
        var device = Substitute.For<BleDevice>((ILogger?)null);
        Action act = () => _ = device.Observer;
        act.Should().Throw<NotInitializedException>();
    }

    [Fact]
    [SuppressMessage("Non-substitutable member", "NS1000:Non-virtual setup specification.")]
    [SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
    public async Task Capability_NotSupported_ShouldThrow()
    {
        var device = Substitute.For<BleDevice>((ILogger?)null);
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
        var device = Substitute.For<BleDevice>((ILogger?)null);

        await device.DisposeAsync();

        device.ReceivedWithAnyArgs(1).InvokeNonPublicMethod("DisposeAsyncCore");
        device.ReceivedWithAnyArgs(1).InvokeNonPublicMethod("DisposeCore");
    }
}