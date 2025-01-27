using System.Reactive.Concurrency;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

/// <summary> Provides a mock device to the available devices </summary>
public sealed class BleMockFactory : IBleFactory
{
    /// <summary> Delegate which describes configuration using a broadcaster and a peripheral </summary>
    /// <param name="bleDevice"> The mocked bleDevice </param>
    public delegate Task InitializeAsync(IBleDevice bleDevice);

    private readonly List<(InitializeAsync OnInitialize, string? Name)> _configuredPeripherals = [];

    /// <summary> Adds a new peripheral which can be discovered by the mock </summary>
    /// <param name="onInitialize"> Initialize the mocked peripheral </param>
    /// <param name="name"> The optional name of the mocked peripheral </param>
    /// <returns> The same <see cref="BleMockFactory"/> </returns>
    public BleMockFactory AddPeripheral(InitializeAsync onInitialize, string? name = null)
    {
        _configuredPeripherals.Add((onInitialize, name));
        return this;
    }

    /// <summary> Adds a new central which can discover the mock </summary>
    /// <param name="onInitialize"> Initialize the mocked central </param>
    /// <param name="name"> The optional name of the mocked central </param>
    /// <returns> The same <see cref="BleMockFactory"/> </returns>
    public BleMockFactory AddCentral(InitializeAsync onInitialize, string? name = null)
    {
        return this;
    }

    /// <summary> The name of the resulting device </summary>
    public string Name { get; set; } = "Mock";

    /// <summary> A scheduler to be used whenever time is used </summary>
    public IScheduler? Scheduler { get; set; }

    /// <inheritdoc />
    public IEnumerable<IBleDevice> EnumerateDevices(ILoggerFactory loggerFactory)
    {
        yield return new MockBleDevice(_configuredPeripherals, Name, Scheduler ?? System.Reactive.Concurrency.Scheduler.Default, loggerFactory);
    }
}