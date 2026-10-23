using Darp.Ble.Backends;
using Darp.Ble.Mock;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Darp.Ble.Tests.Lifecycle;

/// <summary> The adapter lifecycle of the core, through the mock backend where it can show the behaviour </summary>
public sealed class BleManagerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task EnumerateAdaptersAsync_ListsTheAdaptersOfAllBackendsInRegistrationOrder()
    {
        var manager = new BleManager(builder =>
            builder.AddMock(options => options.Name = "First").AddMock(options => options.Name = "Second")
        );

        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);

        adapters.Select(adapter => adapter.Name).ShouldBe(["First", "Second"]);
    }

    [Fact]
    public async Task EnumerateAdaptersAsync_WhenABackendFails_ThrowsItsException()
    {
        var failure = new InvalidOperationException("Probing failed");
        var manager = new BleManager(builder => builder.AddBackend(new TestBackend(() => throw failure)));

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            manager.EnumerateAdaptersAsync(Token)
        );

        exception.ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task EnumerateAdaptersAsync_WhenCancelled_ThrowsTheCallersCancellation()
    {
        var manager = new BleManager(builder => builder.AddMock());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(() =>
            manager.EnumerateAdaptersAsync(cancellation.Token)
        );

        exception.CancellationToken.ShouldBe(cancellation.Token);
    }

    [Fact]
    public async Task OpenAsync_WhenTheAdapterCannotBeOpened_ThrowsBleOpenExceptionAndLogsIt()
    {
        var loggerFactory = new RecordingLoggerFactory();
        BleAdapterInfo info = await SingleMockAdapterAsync(
            options => options.OpenFailure = BleOpenErrorKind.Disabled,
            loggerFactory
        );

        BleOpenException exception = await Should.ThrowAsync<BleOpenException>(() => info.OpenAsync(Token));

        exception.Kind.ShouldBe(BleOpenErrorKind.Disabled);
        RecordingLoggerFactory.Entry entry = loggerFactory.Entries.ShouldHaveSingleItem();
        entry.Category.ShouldBe("Darp.Ble");
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.EventId.Id.ShouldBe(1002);
        entry.EventId.Name.ShouldBe("AdapterOpenFailed");
        entry.Exception.ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task OpenAsync_WhenCancelled_ThrowsTheCallersCancellationWithoutLoggingAFailure()
    {
        var loggerFactory = new RecordingLoggerFactory();
        BleAdapterInfo info = await SingleMockAdapterAsync(configure: null, loggerFactory);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(() =>
            info.OpenAsync(cancellation.Token)
        );

        exception.CancellationToken.ShouldBe(cancellation.Token);
        loggerFactory.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task OpenAndDisposeAsync_LogTheAdapterOnceEach()
    {
        var loggerFactory = new RecordingLoggerFactory();
        BleAdapterInfo info = await SingleMockAdapterAsync(configure: null, loggerFactory);

        BleAdapter adapter = await info.OpenAsync(Token);
        await adapter.DisposeAsync();
        await adapter.DisposeAsync();

        loggerFactory
            .Entries.Select(entry => (entry.EventId.Id, entry.EventId.Name))
            .ShouldBe([(1000, "AdapterOpened"), (1001, "AdapterDisposed")]);
    }

    [Fact]
    public async Task OpenAsync_WhenTheAdapterCannotBeCreated_AwaitsDisposingTheOpenedDriver()
    {
        var driver = new TestAdapterDriver();
        var manager = new BleManager(
            builder => builder.AddBackend(new TestBackend(driver)),
            new RecordingLoggerFactory(throwOnLog: true)
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);

        Task<BleAdapter> opening = adapters[0].OpenAsync(Token);

        opening.IsCompleted.ShouldBeFalse();
        driver.DisposeCount.ShouldBe(1);
        driver.CompleteDisposal();
        await Should.ThrowAsync<InvalidOperationException>(() => opening);
    }

    [Fact]
    public async Task DisposeAsync_AwaitsTheDriversDisposalAndRunsItOnce()
    {
        var driver = new TestAdapterDriver();
        var manager = new BleManager(builder => builder.AddBackend(new TestBackend(driver)));
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        BleAdapter adapter = await adapters[0].OpenAsync(Token);

        Task firstDisposal = adapter.DisposeAsync().AsTask();
        Task secondDisposal = adapter.DisposeAsync().AsTask();

        firstDisposal.IsCompleted.ShouldBeFalse();
        secondDisposal.IsCompleted.ShouldBeFalse();
        driver.CompleteDisposal();
        await Task.WhenAll(firstDisposal, secondDisposal);
        driver.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public async Task AddBleManager_RegistersOneManagerWithTheConfiguredBackends()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddBleManager(builder => builder.AddMock(options => options.Name = "Registered"))
            .BuildServiceProvider();
        BleManager manager = provider.GetRequiredService<BleManager>();

        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);

        provider.GetRequiredService<BleManager>().ShouldBeSameAs(manager);
        adapters.ShouldHaveSingleItem().Name.ShouldBe("Registered");
    }

    private static async Task<BleAdapterInfo> SingleMockAdapterAsync(
        Action<MockBackendOptions>? configure,
        ILoggerFactory loggerFactory
    )
    {
        var manager = new BleManager(builder => builder.AddMock(configure), loggerFactory);
        IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(Token);
        return adapters.ShouldHaveSingleItem();
    }
}
