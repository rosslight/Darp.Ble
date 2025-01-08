using Microsoft.Extensions.Logging;

namespace Darp.Ble.Examples.Unix.Mockup;

public sealed class BMFactory : IBleFactory
{
    public string Name { get; set; } = "Mock";

    public IEnumerable<IBleDevice> EnumerateDevices(ILogger? logger)
    {
        yield return new BMDevice(Name, logger);
    }
}