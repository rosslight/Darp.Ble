using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock;

/// <summary> Provides windows specific implementation of a ble device </summary>
public sealed class MockBleDevice(Func<BleBroadcasterMock, MockBlePeripheral, Task> configure) : IPlatformSpecificBleDevice
{
    private readonly Func<BleBroadcasterMock, MockBlePeripheral, Task> _configure = configure;
    private readonly BleBroadcasterMock _broadcaster = new();
    private readonly MockBlePeripheral _peripheral = new();

    /// <param name="cancellationToken"></param>
    /// <inheritdoc />
    public async Task<InitializeResult> InitializeAsync(CancellationToken cancellationToken)
    {
        Observer = new MockBleObserver(_broadcaster);
        Central = new MockBleCentral(_peripheral);
        await _configure.Invoke(_broadcaster, _peripheral);
        return InitializeResult.Success;
    }

    /// <inheritdoc />
    public string Name => "Mock";
    /// <inheritdoc />
    public IPlatformSpecificBleObserver? Observer { get; private set; }
    /// <inheritdoc />
    public IPlatformSpecificBleCentral? Central { get; private set; }
    /// <inheritdoc />
    public IPlatformSpecificBlePeripheral? Peripheral { get; }

    /// <inheritdoc />
#pragma warning disable CA1822
    public string Identifier => "Darp.Ble.Mock";
#pragma warning restore CA1822

    void IDisposable.Dispose()
    {
    }
}