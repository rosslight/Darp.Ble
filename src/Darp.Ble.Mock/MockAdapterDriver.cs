using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Mock.Connections;
using Darp.Ble.Mock.Gap;

namespace Darp.Ble.Mock;

/// <summary> A mock adapter or simulated peripheral: scans and advertises in its air </summary>
internal sealed class MockAdapterDriver : IBleAdapterDriver
{
    private readonly List<BleAdapter> _peripherals = [];
    private BleAddress _randomAddress = BleAddress.NewRandomStaticAddress();

    public MockAdapterDriver(MockAir air)
    {
        Observer = new MockObserverDriver(air, this);
        Broadcaster = new MockBroadcasterDriver(air, this);
        Central = new MockCentralDriver(air, this);
    }

    public Capabilities Capabilities =>
        Capabilities.Observer | Capabilities.Broadcaster | Capabilities.Central | Capabilities.SetRandomAddress;

    public IObserverDriver? Observer { get; }

    public IBroadcasterDriver? Broadcaster { get; }

    public ICentralDriver? Central { get; }

    public BleAddress RandomAddress => _randomAddress;

    public Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken)
    {
        _randomAddress = address;
        return Task.CompletedTask;
    }

    public void AddPeripheral(BleAdapter peripheral) => _peripherals.Add(peripheral);

    /// <summary> Disposes the simulated peripherals; tries all of them even if one fails </summary>
    public async ValueTask DisposeAsync()
    {
        List<Exception>? failures = null;
        foreach (BleAdapter peripheral in _peripherals)
        {
            try
            {
                await peripheral.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                (failures ??= []).Add(e);
            }
        }
        if (failures is not null)
            throw new AggregateException("Not every mock peripheral could be disposed", failures);
    }
}
