using Microsoft.Extensions.Logging;

namespace Darp.Ble;

/// <summary> Configure the ble manager. Add new implementations or specify logging behavior </summary>
public sealed class BleManagerBuilder
{
    private readonly List<IBleFactory> _factories = [];
    private ILogger? _logger;

    /// <summary> Add a new factory </summary>
    /// <param name="config"> An optional callback to modify the implementation config </param>
    /// <typeparam name="TFactory"> The type of the factory </typeparam>
    /// <returns> The current builder </returns>
    public BleManagerBuilder Add<TFactory>(Action<TFactory>? config = null)
        where TFactory : IBleFactory, new()
    {
        var factory = new TFactory();
        config?.Invoke(factory);
        return Add(factory);
    }

    /// <summary> Add a new factory directly </summary>
    /// <param name="factory"> The factory to be added </param>
    /// <returns> The current builder  </returns>
    public BleManagerBuilder Add(IBleFactory factory)
    {
        _factories.Add(factory);
        return this;
    }

    /// <summary> Register a handler for log events </summary>
    /// <param name="logger"> Called when any part of the ble library logs something </param>
    /// <returns> The current builder </returns>
    public BleManagerBuilder SetLogger(ILogger logger)
    {
        _logger = logger;
        return this;
    }

    /// <summary> Create a new ble manager </summary>
    /// <returns> The new ble manager </returns>
    public BleManager CreateManager()
    {
        return new BleManager(_factories, _logger);
    }
}