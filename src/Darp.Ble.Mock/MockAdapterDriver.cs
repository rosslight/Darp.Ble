using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Mock;

internal sealed class MockAdapterDriver : IBleAdapterDriver
{
    public Capabilities Capabilities => Capabilities.Central | Capabilities.Peripheral;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
