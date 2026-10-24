using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Mock.Gap;

internal sealed class MockBroadcasterDriver(MockAir air, MockAdapterDriver adapter) : IBroadcasterDriver
{
    private readonly MockAir _air = air;
    private readonly MockAdapterDriver _adapter = adapter;

    public Task<IAdvertisingSetDriver> CreateAdvertisingSetAsync(
        AdvertisingParameters parameters,
        AdvertisingData data,
        AdvertisingData? scanResponseData,
        IAdvertisingSetSink sink,
        CancellationToken cancellationToken
    )
    {
        var state = new MockAdvertisingState(parameters, _adapter.RandomAddress, data, scanResponseData);
        return Task.FromResult<IAdvertisingSetDriver>(new MockAdvertisingSetDriver(_air, _adapter, state, sink));
    }
}
