using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Mock.Gap;

namespace Darp.Ble.Mock.Connections;

/// <summary> Connects to sets that advertise connectable in the air; anything else is unreachable at once </summary>
internal sealed class MockCentralDriver(MockAir air, MockAdapterDriver adapter) : ICentralDriver
{
    private readonly MockAir _air = air;
    private readonly MockAdapterDriver _adapter = adapter;

    public async Task<IConnectionDriver> ConnectAsync(
        BleAddress address,
        BleConnectionParameters parameters,
        IConnectionSink sink,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        MockAdvertisingSetDriver? set = _air.FindConnectable(_adapter, address);
        MockLink? link = set is null
            ? null
            : await set.ConnectAsync(_adapter.RandomAddress, sink).ConfigureAwait(false);
        return link?.Central
            ?? throw new BleConnectException(
                BleConnectErrorKind.Unreachable,
                $"No mock peripheral advertises connectable as {address}"
            );
    }
}
