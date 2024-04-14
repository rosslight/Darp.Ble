using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock;

/// <summary> Provides windows specific implementation of a ble device </summary>
public sealed class MockBleDevice(BleBroadcasterMock broadcaster, MockBlePeripheral peripheral) : IPlatformSpecificBleDevice
{
    private readonly BleBroadcasterMock _broadcaster = broadcaster;
    private readonly MockBlePeripheral _peripheral = peripheral;

    /// <inheritdoc />
    public Task<InitializeResult> InitializeAsync()
    {
        Observer = new MockBleObserver(_broadcaster);
        Central = new MockBleCentral(_peripheral);
        return Task.FromResult(InitializeResult.Success);
    }

    /// <inheritdoc />
    public string Name => "Mock";
    /// <inheritdoc />
    public IPlatformSpecificBleObserver? Observer { get; private set; }
    /// <inheritdoc />
    public IPlatformSpecificBleCentral? Central { get; private set; }

    /// <inheritdoc />
#pragma warning disable CA1822
    public string Identifier => "Darp.Ble.Mock";
#pragma warning restore CA1822

    void IDisposable.Dispose()
    {
    }
}