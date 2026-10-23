using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Tests.Lifecycle;

/// <summary> An adapter driver with the roles a test gives it. Its disposal can wait until the test says so </summary>
internal sealed class TestAdapterDriver : IBleAdapterDriver
{
    private readonly TaskCompletionSource _allowDisposal = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Capabilities Capabilities { get; init; }

    public IObserverDriver? Observer { get; init; }

    public IBroadcasterDriver? Broadcaster { get; init; }

    public ICentralDriver? Central { get; init; }

    /// <summary> When set, changing the address waits until the test completes it </summary>
    public TaskCompletionSource? HoldAddressChange { get; init; }

    /// <summary> When true, disposal waits for <see cref="CompleteDisposal"/> </summary>
    public bool HoldDisposal { get; init; }

    public BleAddress RandomAddress { get; private set; } = BleAddress.NotAvailable;

    public int DisposeCount { get; private set; }

    public async Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken)
    {
        if (HoldAddressChange is not null)
            await HoldAddressChange.Task.ConfigureAwait(false);
        RandomAddress = address;
    }

    public void CompleteDisposal() => _allowDisposal.TrySetResult();

    public async ValueTask DisposeAsync()
    {
        DisposeCount++;
        if (HoldDisposal)
            await _allowDisposal.Task.ConfigureAwait(false);
    }
}
