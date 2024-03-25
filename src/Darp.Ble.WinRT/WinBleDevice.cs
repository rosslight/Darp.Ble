using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.WinRT;

public sealed class WinBleDevice : IPlatformSpecificBleDevice
{
    public Task<InitializeResult> InitializeAsync()
    {
        Observer = new WinBleObserver();
        return Task.FromResult(InitializeResult.Success);
    }

    public IPlatformSpecificBleObserver? Observer { get; private set; }
    public string Identifier => "Darp.Ble.WinRT";
}