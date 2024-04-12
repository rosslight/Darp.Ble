using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.WinRT;

/// <summary> Provides windows specific implementation of a ble device </summary>
public sealed class WinBleDevice : IPlatformSpecificBleDevice
{
    /// <inheritdoc />
    public Task<InitializeResult> InitializeAsync()
    {
        Observer = new WinBleObserver();
        Central = new WinBleCentral();
        return Task.FromResult(InitializeResult.Success);
    }

    /// <inheritdoc />
    public string Name => "Windows";
    /// <inheritdoc />
    public IPlatformSpecificBleObserver? Observer { get; private set; }
    /// <inheritdoc />
    public IPlatformSpecificBleCentral? Central { get; private set; }

    /// <inheritdoc />
#pragma warning disable CA1822
    public string Identifier => "Darp.Ble.WinRT";
#pragma warning restore CA1822

    void IDisposable.Dispose()
    {
    }
}