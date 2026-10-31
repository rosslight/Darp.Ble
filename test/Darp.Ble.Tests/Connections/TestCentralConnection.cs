using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Tests.Connections;

internal sealed class TestCentralConnection(BleAddress address) : ICentralConnectionDriver
{
    public BleAddress Address { get; } = address;
}
