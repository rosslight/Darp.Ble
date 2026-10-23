using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Tests.Scanning;

internal sealed class TestScanAdapterDriver(TestObserverDriver observer) : IBleAdapterDriver
{
    private readonly TestObserverDriver _observer = observer;

    public Capabilities Features => Capabilities.Observer;

    public IObserverDriver? Observer => _observer;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
