using Darp.Ble.Implementation;

namespace Darp.Ble.Examples.Unix.Mockup;

public class BMFactory : IBleFactory
{
    public string Name { get; set; } = "Mock";

    public IEnumerable<IBleDevice> EnumerateDevices(IObserver<(BleDevice, LogEvent)>? logger)
    {
        yield return new BMDevice(Name, logger);
    }
}