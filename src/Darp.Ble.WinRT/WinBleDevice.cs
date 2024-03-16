using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.WinRT;

public class WinBleDevice : IBleDeviceImplementation
{
    public Task<InitializeResult> InitializeAsync()
    {
        Observer = new WinBleObserver();
        return Task.FromResult(InitializeResult.Success);
    }

    public IBleObserverImplementation? Observer { get; private set; }
}