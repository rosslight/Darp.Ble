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
        parameters ??= AdvertisingParameters.Default;
        data ??= AdvertisingData.Empty;
        if (!parameters.Type.HasFlag(BleEventType.Legacy)
            && parameters.Type.HasFlag(BleEventType.Connectable) &&
            parameters.Type.HasFlag(BleEventType.Scannable))
        {
            throw new ArgumentOutOfRangeException(nameof(parameters),
                "Non-legacy extended advertising event properties may not be both connectable and scannable");
        }
        return CreateAdvertisingSetAsyncCore(parameters, data, scanResponseData, cancellationToken);
    }

    /// <inheritdoc cref="CreateAdvertisingSetAsync" />
    protected abstract Task<IAdvertisingSet> CreateAdvertisingSetAsyncCore(AdvertisingParameters parameters,
        AdvertisingData data,
        AdvertisingData? scanResponseData,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public Task<IAsyncDisposable> StartAdvertisingAsync(
        IReadOnlyCollection<(IAdvertisingSet AdvertisingSet, TimeSpan Duration, byte NumberOfEvents)> advertisingSet,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(advertisingSet);

        foreach ((_, TimeSpan duration, int numberOfEvents) in advertisingSet)
        {
            if (duration > TimeSpan.Zero && numberOfEvents > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(advertisingSet), "Cannot have both duration and numberOfEvents > 0");
            }
        }

        return StartAdvertisingCoreAsync(advertisingSet, cancellationToken);
    }

    /// <summary> Start advertising multiple advertising sets </summary>
    /// <param name="advertisingSets"> A collection of advertising sets together with information on how to start them </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> An async disposable to stop advertising </returns>
    protected abstract Task<IAsyncDisposable> StartAdvertisingCoreAsync(
        IReadOnlyCollection<(IAdvertisingSet AdvertisingSet, TimeSpan Duration, byte NumberOfEvents)> advertisingSets,
        CancellationToken cancellationToken);

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