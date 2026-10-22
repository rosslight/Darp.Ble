using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Tests.Lifecycle;

/// <summary> A driver whose disposal finishes when the test says so </summary>
internal sealed class TestAdapterDriver(Capabilities capabilities) : IBleAdapterDriver
{
    private readonly TaskCompletionSource _allowDisposal = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TestAdapterDriver(Capabilities capabilities, bool completeDisposalImmediately)
        : this(capabilities)
    {
        if (completeDisposalImmediately)
            _allowDisposal.SetResult();
    }

    public Capabilities Features { get; } = capabilities;

    public int DisposeCount { get; private set; }

    public void CompleteDisposal() => _allowDisposal.SetResult();

    public async ValueTask DisposeAsync()
    {
        DisposeCount++;
        await _allowDisposal.Task.ConfigureAwait(false);
    }
}
