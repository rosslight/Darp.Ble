using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Implementation;

/// <summary> The broadcaster view of a ble device </summary>
public abstract class BleBroadcaster(ILogger? logger) : IBleBroadcaster
{
    private readonly List<IAdvertisingSet> _advertisingSets = [];

    /// <summary> The logger </summary>
    protected ILogger? Logger { get; } = logger;

    public IReadOnlyCollection<IAdvertisingSet> AdvertisingSets => _advertisingSets.AsReadOnly();

    /// <inheritdoc />
    public async Task<IAdvertisingSet> CreateAdvertisingSetAsync(AdvertisingParameters? parameters = null,
        AdvertisingData? data = null,
        AdvertisingData? scanResponseData = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters is not null && !parameters.Type.HasFlag(BleEventType.Legacy)
            && parameters.Type.HasFlag(BleEventType.Connectable) &&
            parameters.Type.HasFlag(BleEventType.Scannable))
        {
            throw new ArgumentOutOfRangeException(nameof(parameters),
                "Non-legacy extended advertising event properties may not be both connectable and scannable");
        }
        IAdvertisingSet advertisingSet = await CreateAdvertisingSetAsyncCore(parameters, data, scanResponseData, cancellationToken).ConfigureAwait(false);
        _advertisingSets.Add(advertisingSet);
        return advertisingSet;
    }

    /// <summary> Removes an advertising set from the current broadcaster. Only is intended to be called by the advertising set on disposal </summary>
    /// <param name="advertisingSet"> The advertising set to be removed </param>
    /// <returns> True if item is successfully removed; otherwise, False </returns>
    internal bool RemoveAdvertisingSet(IAdvertisingSet advertisingSet)
    {
        return _advertisingSets.Remove(advertisingSet);
    }

    /// <inheritdoc cref="CreateAdvertisingSetAsync" />
    protected abstract Task<IAdvertisingSet> CreateAdvertisingSetAsyncCore(AdvertisingParameters? parameters,
        AdvertisingData? data,
        AdvertisingData? scanResponseData,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public Task<IAsyncDisposable> StartAdvertisingAsync(
        IReadOnlyCollection<(IAdvertisingSet AdvertisingSet, TimeSpan Duration, byte NumberOfEvents)> advertisingSetStartInfo,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(advertisingSetStartInfo);

        foreach ((IAdvertisingSet set, TimeSpan duration, int numberOfEvents) in advertisingSetStartInfo)
        {
            if (set.Broadcaster != this)
            {
                throw new ArgumentOutOfRangeException(nameof(advertisingSetStartInfo), "Cannot start an advertising set for this broadcaster if the set has a different broadcaster configured");
            }
            if (duration > TimeSpan.Zero && numberOfEvents > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(advertisingSetStartInfo), "Cannot have both duration and numberOfEvents > 0");
            }
        }

        return StartAdvertisingCoreAsync(advertisingSetStartInfo, cancellationToken);
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