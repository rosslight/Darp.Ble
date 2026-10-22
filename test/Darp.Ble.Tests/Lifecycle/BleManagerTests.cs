using Darp.Ble.Backends;
using Darp.Ble.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Darp.Ble.Tests.Lifecycle;

public sealed class BleManagerTests
{
    private static TestAdapterDriver ReadyDriver(Capabilities capabilities = Capabilities.Central) =>
        new(capabilities, completeDisposalImmediately: true);

    [Fact]
    public async Task EnumerateAdaptersAsync_ListsTheAdaptersOfAllBackendsInRegistrationOrder()
    {
        var first = new TestBackend(
            "First",
            new TestAdapterSource("A", ReadyDriver()),
            new TestAdapterSource("B", ReadyDriver())
        );
        var second = new TestBackend("Second", new TestAdapterSource(name: null, ReadyDriver()));
        var manager = new BleManager(builder => builder.AddBackend(first).AddBackend(second));

        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(
            TestContext.Current.CancellationToken
        );

        adapters.Select(a => (a.Backend, a.Name)).ShouldBe([("First", "A"), ("First", "B"), ("Second", null)]);
    }

    [Fact]
    public async Task EnumerateAdaptersAsync_WhenABackendFails_ThrowsItsException()
    {
        var failure = new InvalidOperationException("Probing failed");
        var manager = new BleManager(builder => builder.AddBackend(new FailingBackend(failure)));

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            manager.EnumerateAdaptersAsync(TestContext.Current.CancellationToken)
        );

        exception.ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task OpenAsync_ReturnsAnAdapterWithTheDriversCapabilities()
    {
        BleAdapterInfo info = await SingleAdapterAsync(
            new TestAdapterSource("A", ReadyDriver(Capabilities.Peripheral))
        );

        await using BleAdapter adapter = await info.OpenAsync(TestContext.Current.CancellationToken);

        adapter.Backend.ShouldBe("Test");
        adapter.Name.ShouldBe("A");
        adapter.Capabilities.ShouldBe(Capabilities.Peripheral);
    }

    [Theory]
    [InlineData(BleOpenErrorKind.NotAvailable, true, 16)]
    [InlineData(BleOpenErrorKind.Disabled, false, null)]
    [InlineData(BleOpenErrorKind.MissingPermission, false, 5)]
    public async Task OpenAsync_WhenTheBackendCannotOpen_ThrowsItsBleOpenException(
        BleOpenErrorKind kind,
        bool isTransient,
        int? nativeCode
    )
    {
        var cause = new IOException("Port busy");
        var failure = new BleOpenException(kind, "Cannot open", isTransient, nativeCode, cause);
        var loggerFactory = new RecordingLoggerFactory();
        BleAdapterInfo info = await SingleAdapterAsync(new TestAdapterSource("A", _ => throw failure), loggerFactory);

        BleOpenException exception = await Should.ThrowAsync<BleOpenException>(() =>
            info.OpenAsync(TestContext.Current.CancellationToken)
        );

        exception.ShouldBeSameAs(failure);
        exception.Kind.ShouldBe(kind);
        exception.IsTransient.ShouldBe(isTransient);
        exception.NativeCode.ShouldBe(nativeCode);
        exception.InnerException.ShouldBeSameAs(cause);
        loggerFactory
            .Entries.ShouldHaveSingleItem()
            .ShouldBe(("Darp.Ble", LogLevel.Warning, new EventId(1002, "AdapterOpenFailed"), failure));
    }

    [Fact]
    public async Task OpenAsync_WhenCancelled_ThrowsTheCallersCancellation()
    {
        var loggerFactory = new RecordingLoggerFactory();
        BleAdapterInfo info = await SingleAdapterAsync(
            new TestAdapterSource(
                "A",
                async cancellationToken =>
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                    return ReadyDriver();
                }
            ),
            loggerFactory
        );
        using var cancellation = new CancellationTokenSource();

        Task<BleAdapter> opening = info.OpenAsync(cancellation.Token);
        await cancellation.CancelAsync();

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(() => opening);
        exception.CancellationToken.ShouldBe(cancellation.Token);
        loggerFactory.Entries.ShouldBeEmpty(); // cancelling is not an open failure
    }

    [Fact]
    public async Task OpenAsync_WhenTheAdapterCannotBeCreated_DisposesTheOpenedDriver()
    {
        var driver = ReadyDriver();
        BleAdapterInfo info = await SingleAdapterAsync(
            new TestAdapterSource("A", driver),
            new RecordingLoggerFactory(throwOnLog: true)
        );

        await Should.ThrowAsync<InvalidOperationException>(() => info.OpenAsync(TestContext.Current.CancellationToken));

        driver.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public async Task DisposeAsync_AwaitsTheDriversDisposalAndRunsItOnce()
    {
        var driver = new TestAdapterDriver(Capabilities.Central);
        BleAdapterInfo info = await SingleAdapterAsync(new TestAdapterSource("A", driver));
        BleAdapter adapter = await info.OpenAsync(TestContext.Current.CancellationToken);

        Task firstDisposal = adapter.DisposeAsync().AsTask();
        Task secondDisposal = adapter.DisposeAsync().AsTask();

        firstDisposal.IsCompleted.ShouldBeFalse();
        secondDisposal.IsCompleted.ShouldBeFalse();
        driver.CompleteDisposal();
        await Task.WhenAll(firstDisposal, secondDisposal);
        driver.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public async Task Constructor_GivesItsLoggerFactoryAndTimeProviderToBackendsAndDrivers()
    {
        using var loggerFactory = new LoggerFactory();
        var timeProvider = new TestTimeProvider();
        var source = new TestAdapterSource("A", ReadyDriver());
        var backend = new TestBackend("Test", source);
        var manager = new BleManager(builder => builder.AddBackend(backend), loggerFactory, timeProvider);

        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(
            TestContext.Current.CancellationToken
        );
        await using BleAdapter adapter = await adapters[0].OpenAsync(TestContext.Current.CancellationToken);

        ShouldHaveServices(backend.EnumerationContext, loggerFactory, timeProvider);
        ShouldHaveServices(source.OpenContext, loggerFactory, timeProvider);
    }

    [Fact]
    public async Task Constructor_WithoutServices_UsesNoLoggingAndTheSystemClock()
    {
        var backend = new TestBackend("Test");
        var manager = new BleManager(builder => builder.AddBackend(backend));

        await manager.EnumerateAdaptersAsync(TestContext.Current.CancellationToken);

        ShouldHaveServices(backend.EnumerationContext, NullLoggerFactory.Instance, TimeProvider.System);
    }

    [Fact]
    public async Task AddBleManager_UsesTheContainersLoggerFactoryAndTimeProvider()
    {
        using var loggerFactory = new LoggerFactory();
        var timeProvider = new TestTimeProvider();
        var source = new TestAdapterSource("A", ReadyDriver());
        var backend = new TestBackend("Test", source);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(loggerFactory)
            .AddSingleton<TimeProvider>(timeProvider)
            .AddBleManager(builder => builder.AddBackend(backend))
            .BuildServiceProvider();
        BleManager manager = provider.GetRequiredService<BleManager>();

        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(
            TestContext.Current.CancellationToken
        );
        await using BleAdapter adapter = await adapters[0].OpenAsync(TestContext.Current.CancellationToken);

        provider.GetRequiredService<BleManager>().ShouldBeSameAs(manager);
        ShouldHaveServices(backend.EnumerationContext, loggerFactory, timeProvider);
        ShouldHaveServices(source.OpenContext, loggerFactory, timeProvider);
    }

    private static async Task<BleAdapterInfo> SingleAdapterAsync(
        TestAdapterSource source,
        ILoggerFactory? loggerFactory = null
    )
    {
        var manager = new BleManager(builder => builder.AddBackend(new TestBackend("Test", source)), loggerFactory);
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(
            TestContext.Current.CancellationToken
        );
        return adapters.ShouldHaveSingleItem();
    }

    private static void ShouldHaveServices(
        BleBackendContext? context,
        ILoggerFactory loggerFactory,
        TimeProvider timeProvider
    )
    {
        context.ShouldNotBeNull();
        context.LoggerFactory.ShouldBeSameAs(loggerFactory);
        context.TimeProvider.ShouldBeSameAs(timeProvider);
    }

    private sealed class FailingBackend(Exception failure) : IBleBackend
    {
        private readonly Exception _failure = failure;

        public string Name => "Failing";

        public Task<IReadOnlyList<IBleAdapterSource>> EnumerateAdaptersAsync(
            BleBackendContext context,
            CancellationToken cancellationToken
        ) => Task.FromException<IReadOnlyList<IBleAdapterSource>>(_failure);
    }

    private sealed class TestTimeProvider : TimeProvider;
}
