using Darp.Ble.Data;
using Darp.Ble.Gap;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Implementation;

/// <summary> The broadcaster view of a ble device </summary>
public abstract class BleBroadcaster(ILogger? logger) : IBleBroadcaster
{
    /// <summary> The logger </summary>
    protected ILogger? Logger { get; } = logger;

    /// <inheritdoc />
    public Task<IAdvertisingSet> CreateAdvertisingSetAsync(AdvertisingParameters? parameters = null,
        AdvertisingData? data = null,
        AdvertisingData? scanResponseData = null,
        CancellationToken cancellationToken = default)
    {
        return CreateAdvertisingSetAsyncCore(parameters, data, scanResponseData, cancellationToken);
    }

    /// <inheritdoc cref="CreateAdvertisingSetAsync" />
    protected abstract Task<IAdvertisingSet> CreateAdvertisingSetAsyncCore(AdvertisingParameters? parameters = null,
        AdvertisingData? data = null,
        AdvertisingData? scanResponseData = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public IAsyncDisposable StartAdvertising(IReadOnlyCollection<(IAdvertisingSet AdvertisingSet, TimeSpan Duration, int NumberOfEvents)> advertisingSet)
    {
        ArgumentNullException.ThrowIfNull(advertisingSet);

        foreach ((_, TimeSpan duration, int numberOfEvents) in advertisingSet)
        {
            if (duration > TimeSpan.Zero && numberOfEvents > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(advertisingSet), "Cannot have both duration and numberOfEvents > 0");
            }
        }

        return StartAdvertisingCore(advertisingSet);
    }

    /// <summary> Start advertising multiple advertising sets </summary>
    /// <param name="advertisingSet"> A collection of advertising sets together with information on how to start them </param>
    /// <returns> An async disposable to stop advertising </returns>
    protected abstract IAsyncDisposable StartAdvertisingCore(
        IEnumerable<(IAdvertisingSet AdvertisingSet, TimeSpan Duration, int NumberOfEvents)> advertisingSet);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        DisposeCore();
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void DisposeCore() { }
}