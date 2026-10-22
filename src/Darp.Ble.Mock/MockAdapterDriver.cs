using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Mock;

internal sealed class MockAdapterDriver : IBleAdapterDriver
{
    public Capabilities Features => Capabilities.None;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
