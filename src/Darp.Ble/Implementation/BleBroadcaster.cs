using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Implementation;

/// <summary> The broadcaster view of a ble device </summary>
public abstract class BleBroadcaster(IBleDevice device, ILogger<BleBroadcaster> logger) : IBleBroadcaster
{
    private readonly List<IAdvertisingSet> _advertisingSets = [];

    /// <summary> The logger </summary>
    protected ILogger<BleBroadcaster> Logger { get; } = logger;

    /// <summary> The service provider </summary>
    protected IServiceProvider ServiceProvider => Device.ServiceProvider;

    public IReadOnlyCollection<IAdvertisingSet> AdvertisingSets => _advertisingSets.AsReadOnly();

    /// <inheritdoc />
    public IBleDevice Device { get; } = device;

    /// <inheritdoc />
    public async Task<IAdvertisingSet> CreateAdvertisingSetAsync(
        AdvertisingParameters? parameters = null,
        AdvertisingData? data = null,
        AdvertisingData? scanResponseData = null,
        CancellationToken cancellationToken = default
    )
    {
        if (
            parameters is not null
            && !parameters.Type.HasFlag(BleEventType.Legacy)
            && parameters.Type.HasFlag(BleEventType.Connectable)
            && parameters.Type.HasFlag(BleEventType.Scannable)
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(parameters),
                "Non-legacy extended advertising event properties may not be both connectable and scannable"
            );
        }
        IAdvertisingSet advertisingSet = await CreateAdvertisingSetAsyncCore(
                parameters,
                data,
                scanResponseData,
                cancellationToken
            )
            .ConfigureAwait(false);
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
    protected abstract Task<IAdvertisingSet> CreateAdvertisingSetAsyncCore(
        AdvertisingParameters? parameters,
        AdvertisingData? data,
        AdvertisingData? scanResponseData,
        CancellationToken cancellationToken
    );

    /// <inheritdoc />
    public Task<IAsyncDisposable> StartAdvertisingAsync(
        IReadOnlyCollection<(
            IAdvertisingSet AdvertisingSet,
            TimeSpan Duration,
            byte NumberOfEvents
        )> advertisingSetStartInfo,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(advertisingSetStartInfo);

        foreach ((IAdvertisingSet set, TimeSpan duration, int numberOfEvents) in advertisingSetStartInfo)
        {
            if (set.Broadcaster != this)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(advertisingSetStartInfo),
                    "Cannot start an advertising set for this broadcaster if the set has a different broadcaster configured"
                );
            }
            if (duration > TimeSpan.Zero && numberOfEvents > 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(advertisingSetStartInfo),
                    "Cannot have both duration and numberOfEvents > 0"
                );
            }
        }

        return StartAdvertisingCoreAsync(advertisingSetStartInfo, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> StopAdvertisingAsync(
        IReadOnlyCollection<IAdvertisingSet> advertisingSets,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(advertisingSets);
        return StopAdvertisingCoreAsync(advertisingSets, cancellationToken);
    }

    /// <summary> Start advertising multiple advertising sets </summary>
    /// <param name="advertisingSets"> A collection of advertising sets together with information on how to start them </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> An async disposable to stop advertising </returns>
    protected abstract Task<IAsyncDisposable> StartAdvertisingCoreAsync(
        IReadOnlyCollection<(IAdvertisingSet AdvertisingSet, TimeSpan Duration, byte NumberOfEvents)> advertisingSets,
        CancellationToken cancellationToken
    );

    /// <summary> Stop advertising multiple advertising sets. </summary>
    /// <param name="advertisingSets"> A collection of advertising sets </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> A task </returns>
    protected abstract Task<bool> StopAdvertisingCoreAsync(
        IReadOnlyCollection<IAdvertisingSet> advertisingSets,
        CancellationToken cancellationToken
    );

    /// <summary> A method that can be used to clean up all resources. </summary>
    /// <remarks> This method is not glued to the <see cref="IAsyncDisposable"/> interface. All disposes should be done using the  </remarks>
    internal async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
    }

    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

    /// <inheritdoc cref="IDisposable.Dispose"/>
    /// <param name="disposing">
    /// True, when this method was called by the synchronous <see cref="IDisposable.Dispose"/> method;
    /// False if called by the asynchronous <see cref="IAsyncDisposable.DisposeAsync"/> method
    /// </param>
    protected virtual void Dispose(bool disposing) { }
}
