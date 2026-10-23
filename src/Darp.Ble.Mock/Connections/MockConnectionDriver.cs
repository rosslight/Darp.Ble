using Darp.Ble.Backends;

namespace Darp.Ble.Mock.Connections;

/// <summary> The central's side of a mock link; the peripheral answers at once </summary>
internal sealed class MockConnectionDriver(MockLink link) : IConnectionDriver
{
    /// <summary> The largest MTU the mock peripheral accepts, as many controllers do </summary>
    private const int PeripheralMtu = 247;

    private readonly MockLink _link = link;

    public Task<int> ExchangeMtuAsync(int clientMtu, GattRequest request) =>
        Task.FromResult(Math.Min(clientMtu, PeripheralMtu));

    public ValueTask DisposeAsync()
    {
        _link.DisconnectFromCentral();
        return ValueTask.CompletedTask;
    }
}
