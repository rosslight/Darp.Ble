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
        return Task.FromResult(InitializeResult.Success);
    }

    /// <inheritdoc />
    public IPlatformSpecificBleObserver? Observer { get; private set; }
    /// <inheritdoc />
    public object Central => throw new NotImplementedException();

    /// <inheritdoc />
#pragma warning disable CA1822
    public string Identifier => "Darp.Ble.WinRT";
#pragma warning restore CA1822

    void IDisposable.Dispose()
    {
    }
}