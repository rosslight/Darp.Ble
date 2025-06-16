namespace Darp.Ble;

/// <summary> The base manager class. Holds all implementations </summary>
/// <param name="factories"> All factories to consider when enumerating devices </param>
/// <param name="serviceProvider"> The service provider </param>
public sealed class BleManager(IReadOnlyCollection<IBleFactory> factories, IServiceProvider serviceProvider)
{
    private readonly IReadOnlyCollection<IBleFactory> _factories = factories;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <summary> Enumerate all implementations for devices </summary>
    /// <returns> A list of all available devices </returns>
    public IEnumerable<IBleDevice> EnumerateDevices() =>
        _factories.SelectMany(x => x.EnumerateDevices(_serviceProvider));
}
