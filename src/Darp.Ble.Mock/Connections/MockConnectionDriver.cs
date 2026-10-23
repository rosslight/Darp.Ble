using Darp.Ble.Backends;

namespace Darp.Ble.Mock.Connections;

/// <summary> The central's side of a link to a mock peripheral </summary>
internal sealed class MockConnectionDriver : IConnectionDriver
{
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
