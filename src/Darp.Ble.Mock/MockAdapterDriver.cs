using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Mock;

internal sealed class MockAdapterDriver : IBleAdapterDriver
{
    public Capabilities Capabilities => Capabilities.None;

    public IObserverDriver? Observer => null;

    public IBroadcasterDriver? Broadcaster => null;

    public BleAddress RandomAddress => BleAddress.NotAvailable;

    public Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The mock adapter cannot set its address yet");

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
