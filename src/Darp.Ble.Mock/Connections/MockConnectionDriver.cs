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

    /// <summary> The GAP service first, as on a real peripheral, then the published primary services </summary>
    public Task<IReadOnlyList<IRemoteServiceDriver>> DiscoverServicesAsync(GattRequest request) =>
        Task.FromResult<IReadOnlyList<IRemoteServiceDriver>>([
            _link.GapService,
            .. _link.PeripheralDriver.Services.Where(service => service.IsPrimary).Select(_link.GetService),
        ]);

    public ValueTask DisposeAsync()
    {
        _link.DisconnectFromCentral();
        return ValueTask.CompletedTask;
    }
}
