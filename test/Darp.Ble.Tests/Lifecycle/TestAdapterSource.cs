using Darp.Ble.Backends;

namespace Darp.Ble.Tests.Lifecycle;

/// <summary> An adapter that opens the way a test configures it </summary>
internal sealed class TestAdapterSource(string? name, Func<CancellationToken, Task<IBleAdapterDriver>> open)
    : IBleAdapterSource
{
    private readonly Func<CancellationToken, Task<IBleAdapterDriver>> _open = open;

    public TestAdapterSource(string? name, IBleAdapterDriver driver)
        : this(name, _ => Task.FromResult(driver)) { }

    public string? Name { get; } = name;

    public BleBackendContext? OpenContext { get; private set; }

    public Task<IBleAdapterDriver> OpenAsync(BleBackendContext context, CancellationToken cancellationToken)
    {
        OpenContext = context;
        return _open(cancellationToken);
    }
}
