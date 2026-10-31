using Darp.Ble.Backends;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gap;

/// <summary> The broadcaster role of an adapter: advertises with advertising sets </summary>
public sealed class BleBroadcaster
{
    private readonly IBroadcasterDriver _driver;
    private readonly ILogger _logger;
    private readonly Lock _lock = new();
    private readonly List<AdvertisingSet> _sets = [];

    // Disposal waits for every creation it admitted
    private readonly OperationTracker _creations = new();

    internal BleBroadcaster(BleAdapter adapter, IBroadcasterDriver driver, ILogger logger)
    {
        Adapter = adapter;
        _driver = driver;
        _logger = logger;
    }

    /// <summary> The adapter this broadcaster belongs to </summary>
    public BleAdapter Adapter { get; }

    /// <summary> Creates a stopped advertising set; start it with <see cref="AdvertisingSet.StartAsync"/> </summary>
    /// <param name="parameters"> How the set advertises </param>
    /// <param name="data"> The advertising data; empty when null </param>
    /// <param name="scanResponseData"> The scan response data, if the set has one </param>
    /// <param name="cancellationToken"> Cancels creating </param>
    /// <returns> The set. Disposing it stops and removes it; disposing the adapter disposes it too </returns>
    /// <exception cref="ArgumentException"> The intervals are not positive, or the maximum is shorter than the minimum </exception>
    public Task<AdvertisingSet> CreateAdvertisingSetAsync(
        AdvertisingParameters parameters,
        AdvertisingData? data = null,
        AdvertisingData? scanResponseData = null,
        CancellationToken cancellationToken = default
    ) => CreateCoreAsync(parameters, data, scanResponseData, autoRestart: false, cancellationToken);

    private async Task<AdvertisingSet> CreateCoreAsync(
        AdvertisingParameters parameters,
        AdvertisingData? data,
        AdvertisingData? scanResponseData,
        bool autoRestart,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate(nameof(parameters));
        data ??= AdvertisingData.Empty;
        _creations.Enter(typeof(BleBroadcaster));
        try
        {
            var events = new AdvertisingSetEvents();
            IAdvertisingSetDriver setDriver = await _driver
                .CreateAdvertisingSetAsync(parameters, data, scanResponseData, events, cancellationToken)
                .ConfigureAwait(false);
            var set = new AdvertisingSet(this, setDriver, parameters, data, scanResponseData, autoRestart, _logger);
            events.Attach(set);
            bool closed;
            lock (_lock)
            {
                closed = _creations.IsClosed;
                if (!closed)
                    _sets.Add(set);
            }
            if (closed)
            {
                // The adapter closed while the set was being created; release it before disposal continues
                await set.DisposeAsync().ConfigureAwait(false);
                throw new ObjectDisposedException(nameof(BleBroadcaster));
            }
            return set;
        }
        finally
        {
            _creations.Exit();
        }
    }

    /// <summary> Creates an advertising set and starts it </summary>
    /// <param name="data"> The advertising data </param>
    /// <param name="interval"> How often the set advertises </param>
    /// <param name="type"> The type of the advertising events </param>
    /// <param name="scanResponseData"> The scan response data, if the set has one </param>
    /// <param name="autoRestart"> Whether the set starts again once a central that connected through it disconnected.
    /// A connection always stops the set it came through </param>
    /// <param name="cancellationToken"> Cancels creating and starting </param>
    /// <returns> The advertising set. Disposing it stops advertising </returns>
    public async Task<AdvertisingSet> StartAdvertisingAsync(
        AdvertisingData data,
        TimeSpan interval,
        BleEventType type = BleEventType.AdvInd,
        AdvertisingData? scanResponseData = null,
        bool autoRestart = false,
        CancellationToken cancellationToken = default
    )
    {
        var parameters = new AdvertisingParameters
        {
            Type = type,
            MinPrimaryAdvertisingInterval = interval,
            MaxPrimaryAdvertisingInterval = interval,
        };
        AdvertisingSet set = await CreateCoreAsync(parameters, data, scanResponseData, autoRestart, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            await set.StartAsync(cancellationToken).ConfigureAwait(false);
            return set;
        }
        catch
        {
            await set.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    internal void Remove(AdvertisingSet set)
    {
        lock (_lock)
            _sets.Remove(set);
    }

    /// <summary> Closes admission so no set can be created any more </summary>
    internal void CloseAdmission()
    {
        _creations.Close();
        AdvertisingSet[] sets;
        lock (_lock)
            sets = [.. _sets];
        foreach (AdvertisingSet set in sets)
            set.CloseAdmission();
    }

    /// <summary> Closes admission, waits for creations in flight, then disposes every set; tries all of them even if one fails </summary>
    internal async Task DisposeAsync()
    {
        await _creations.CloseAndWaitAsync().ConfigureAwait(false);
        AdvertisingSet[] sets;
        lock (_lock)
            sets = [.. _sets];
        List<Exception>? failures = null;
        foreach (AdvertisingSet set in sets)
        {
            try
            {
                await set.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                (failures ??= []).Add(e);
            }
        }
        if (failures is not null)
            throw new AggregateException("Not every advertising set could be disposed", failures);
    }
}
