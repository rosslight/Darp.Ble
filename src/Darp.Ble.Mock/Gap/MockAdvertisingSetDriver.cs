using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Mock.Gap;

/// <summary> Sends an advertising event every minimum interval on a timer of the manager's clock </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Usage",
    "CA2213:Disposable fields should be disposed",
    Justification = "The set only names its adapter as the sender; the adapter is not owned by it"
)]
internal sealed class MockAdvertisingSetDriver(MockAir air, MockAdapterDriver adapter, MockAdvertisingState state)
    : IAdvertisingSetDriver
{
    private readonly MockAir _air = air;
    private readonly MockAdapterDriver _adapter = adapter;
    private readonly Lock _lock = new();
    private MockAdvertisingState _state = state;
    private ITimer? _timer;

    public MockAdapterDriver Adapter => _adapter;

    public BleEventType Type
    {
        get
        {
            lock (_lock)
                return _state.Parameters.Type;
        }
    }

    public BleAddress RandomAddress
    {
        get
        {
            lock (_lock)
                return _state.Address;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            TimeSpan interval = _state.Parameters.MinPrimaryAdvertisingInterval;
            if (_timer is not null)
                return Task.CompletedTask;
            _timer = _air.TimeProvider.CreateTimer(_ => Transmit(), state: null, interval, interval);
        }
        _air.StartAdvertising(this);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        ITimer? timer;
        lock (_lock)
        {
            timer = _timer;
            _timer = null;
        }
        if (timer is null)
            return;
        _air.StopAdvertising(this);
        // Waits for a transmission that is still running
        await timer.DisposeAsync().ConfigureAwait(false);
    }

    public Task SetParametersAsync(AdvertisingParameters parameters, CancellationToken cancellationToken) =>
        Update(advertising => advertising with { Parameters = parameters });

    public Task SetDataAsync(AdvertisingData data, CancellationToken cancellationToken) =>
        Update(advertising => advertising with { Data = data });

    public Task SetScanResponseDataAsync(AdvertisingData data, CancellationToken cancellationToken) =>
        Update(advertising => advertising with { ScanResponseData = data });

    public Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken) =>
        Update(advertising => advertising with { Address = address });

    public ValueTask DisposeAsync() => new(StopAsync(CancellationToken.None));

    private void Transmit()
    {
        MockAdvertisingState advertising;
        lock (_lock)
            advertising = _state;
        _air.Transmit(_adapter, advertising);
    }

    private Task Update(Func<MockAdvertisingState, MockAdvertisingState> change)
    {
        lock (_lock)
            _state = change(_state);
        return Task.CompletedTask;
    }
}
