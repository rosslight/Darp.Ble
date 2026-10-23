using Darp.Ble.Backends;
using Darp.Ble.Mock.Gatt;

namespace Darp.Ble.Mock.Connections;

/// <summary> The central's side of a mock link; the peripheral answers at once </summary>
internal sealed class MockConnectionDriver(MockLink link) : IConnectionDriver
{
    /// <summary> The largest MTU the mock peripheral accepts, as many controllers do </summary>
    private const int PeripheralMtu = 247;

    private readonly MockLink _link = link;

    public Task<int> ExchangeMtuAsync(int clientMtu, GattRequest request) =>
        Task.FromResult(Math.Min(clientMtu, PeripheralMtu));

    public Task<IReadOnlyList<IRemoteServiceDriver>> DiscoverServicesAsync(GattRequest request) =>
        Task.FromResult<IReadOnlyList<IRemoteServiceDriver>>([
            .. _link
                .PeripheralDriver.Services.Where(service => service.IsPrimary)
                .Select(service => new MockRemoteService(service)),
        ]);

    public ValueTask DisposeAsync()
    {
        _link.DisconnectFromCentral();
        return ValueTask.CompletedTask;
    }
}
