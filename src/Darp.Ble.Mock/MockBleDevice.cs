using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Mock;

/// <summary> Provides windows specific implementation of a ble device </summary>
public sealed class MockBleDevice(BleBroadcasterMock broadcaster, BlePeripheralMock peripheral) : IPlatformSpecificBleDevice
{
    /// <inheritdoc />
    public Task<InitializeResult> InitializeAsync()
    {
        Observer = new MockBleObserver(broadcaster);
        return Task.FromResult(InitializeResult.Success);
    }

    /// <inheritdoc />
    public string Name => "Mock";
    /// <inheritdoc />
    public IPlatformSpecificBleObserver? Observer { get; private set; }
    /// <inheritdoc />
    public object Central => throw new NotSupportedException();

    /// <inheritdoc />
#pragma warning disable CA1822
    public string Identifier => "Darp.Ble.Mock";
#pragma warning restore CA1822

    void IDisposable.Dispose()
    {
    }
}