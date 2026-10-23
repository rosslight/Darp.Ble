using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Mock;

internal sealed class MockAdapterDriver : IBleAdapterDriver
{
    public Capabilities Capabilities => Capabilities.None;

    public IObserverDriver? Observer => null;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
