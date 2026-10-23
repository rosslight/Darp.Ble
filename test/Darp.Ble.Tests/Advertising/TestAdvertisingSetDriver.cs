using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Tests.Advertising;

/// <summary> An advertising set that records its calls; its broadcaster decides which ones fail </summary>
internal sealed class TestAdvertisingSetDriver(TestBroadcasterDriver broadcaster) : IAdvertisingSetDriver
{
    private readonly TestBroadcasterDriver _broadcaster = broadcaster;

    public List<string> Calls { get; } = [];

    /// <summary> When set, disposal waits until the test completes it </summary>
    public TaskCompletionSource? HoldDisposal { get; set; }

    public BleAddress RandomAddress { get; private set; } = new(BleAddressType.Random, (UInt48)0xC0FFEE);

    public Task StartAsync(CancellationToken cancellationToken) => Record("start");

    public Task StopAsync(CancellationToken cancellationToken) => Record("stop");

    public Task SetParametersAsync(AdvertisingParameters parameters, CancellationToken cancellationToken) =>
        Record("parameters");

    public Task SetDataAsync(AdvertisingData data, CancellationToken cancellationToken) => Record("data");

    public Task SetScanResponseDataAsync(AdvertisingData data, CancellationToken cancellationToken) =>
        Record("scan response");

    public Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken)
    {
        RandomAddress = address;
        return Record("address");
    }

    public async ValueTask DisposeAsync()
    {
        if (HoldDisposal is not null)
            await HoldDisposal.Task.ConfigureAwait(false);
        Calls.Add("dispose");
    }

    private Task Record(string operation)
    {
        if (_broadcaster.Failing.Contains(operation))
            return Task.FromException(new InvalidOperationException($"The controller rejected {operation}"));
        Calls.Add(operation);
        return Task.CompletedTask;
    }
}
