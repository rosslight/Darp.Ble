using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Mock.Connections;

/// <summary> The peripheral's side of a mock link </summary>
internal sealed class MockCentralConnection(MockLink link, BleAddress address) : ICentralConnectionDriver
{
    public MockLink Link { get; } = link;

    public BleAddress Address { get; } = address;
}
