using Darp.Ble.Backends;
using Darp.Ble.Data;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gap;

/// <summary> The broadcaster role of an adapter: advertises with advertising sets </summary>
public sealed class BleBroadcaster
{
    private readonly IBroadcasterDriver _driver;
    private readonly ILogger _logger;
    private readonly Lock _lock = new();
    private readonly List<AdvertisingSet> _sets = [];
    private bool _closed;

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
    public async Task<AdvertisingSet> CreateAdvertisingSetAsync(
        AdvertisingParameters parameters,
        AdvertisingData? data = null,
        AdvertisingData? scanResponseData = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate(nameof(parameters));
        data ??= AdvertisingData.Empty;
        lock (_lock)
            ObjectDisposedException.ThrowIf(_closed, typeof(BleBroadcaster));
        IAdvertisingSetDriver setDriver = await _driver
            .CreateAdvertisingSetAsync(parameters, data, scanResponseData, cancellationToken)
            .ConfigureAwait(false);
        var set = new AdvertisingSet(this, setDriver, parameters, data, scanResponseData, _logger);
        bool closed;
        lock (_lock)
        {
            closed = _closed;
            if (!closed)
                _sets.Add(set);
        }
        if (closed)
        {
            // The adapter was disposed while the set was being created
            await setDriver.DisposeAsync().ConfigureAwait(false);
            throw new ObjectDisposedException(nameof(BleBroadcaster));
        }
        return set;
    }

    /// <summary> Creates an advertising set and starts it </summary>
    /// <param name="data"> The advertising data </param>
    /// <param name="interval"> How often the set advertises </param>
    /// <param name="type"> The type of the advertising events </param>
    /// <param name="scanResponseData"> The scan response data, if the set has one </param>
    /// <param name="cancellationToken"> Cancels creating and starting </param>
    /// <returns> The advertising set. Disposing it stops advertising </returns>
    public async Task<AdvertisingSet> StartAdvertisingAsync(
        AdvertisingData data,
        TimeSpan interval,
        BleEventType type = BleEventType.AdvInd,
        AdvertisingData? scanResponseData = null,
        CancellationToken cancellationToken = default
    )
    {
        var parameters = new AdvertisingParameters
        {
            Type = type,
            MinPrimaryAdvertisingInterval = interval,
            MaxPrimaryAdvertisingInterval = interval,
        };
        AdvertisingSet set = await CreateAdvertisingSetAsync(parameters, data, scanResponseData, cancellationToken)
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

    /// <summary> Closes admission and disposes every set; tries all of them even if one fails </summary>
    internal async Task DisposeAsync()
    {
        AdvertisingSet[] sets;
        lock (_lock)
        {
            _closed = true;
            sets = [.. _sets];
        }
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
