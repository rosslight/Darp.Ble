using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

/// <summary> Provides a mock device to the available devices </summary>
public sealed class BleMockFactory : IBleFactory
{
    /// <summary> Delegate which describes configuration using a broadcaster and a peripheral </summary>
    public delegate Task InitializeAsync(IBleBroadcaster broadcaster, IBlePeripheral peripheral);

    /// <summary> Configuration callback when the mock device is initialized </summary>
    public InitializeAsync? OnInitialize { get; init; }
    /// <summary> The name of the resulting device </summary>
    public string Name { get; set; } = "Mock";

    /// <inheritdoc />
    public IEnumerable<IBleDevice> EnumerateDevices(ILogger? logger)
    {
        InitializeAsync onInitialize = OnInitialize ?? ((_, _) => Task.CompletedTask);
        yield return new MockBleDevice(onInitialize, Name, logger);
    }
}