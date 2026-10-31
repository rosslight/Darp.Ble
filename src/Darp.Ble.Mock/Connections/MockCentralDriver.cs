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

    // Guards the links: the core connects while a test takes the device out of range
    private readonly Lock _lock = new();
    private readonly List<MockLink> _links = [];

    /// <summary> The address peripherals see the central connect from </summary>
    public BleAddress Address => _adapter.RandomAddress;

    /// <summary> The links that are up </summary>
    public MockLink[] Links
    {
        get
        {
            lock (_lock)
                return [.. _links];
        }
    }

    public async Task<IConnectionDriver> ConnectAsync(
        BleAddress address,
        BleConnectionParameters parameters,
        IConnectionSink sink,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        MockAdvertisingSetDriver? set = _air.FindConnectable(_adapter, address);
        MockLink? link = set is null ? null : await set.ConnectAsync(this, sink).ConfigureAwait(false);
        if (set is null || link is null)
        {
            throw new BleConnectException(
                BleConnectErrorKind.Unreachable,
                $"No mock peripheral in range advertises connectable as {address}"
            );
        }
        // Added first, then checked: either a Leave finds the link, or this check finds a device that left
        lock (_lock)
            _links.Add(link);
        // A link that went down meanwhile already forgot itself, before it was added
        if (link.IsDown)
            Forget(link);
        else if (!_adapter.IsInRange || !set.Adapter.IsInRange)
            link.Lose(DisconnectReason.ConnectionTimeout);
        return link.Central;
    }

    /// <summary> A link went down </summary>
    public void Forget(MockLink link)
    {
        lock (_lock)
            _links.Remove(link);
    }
}
