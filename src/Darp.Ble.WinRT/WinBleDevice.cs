using Darp.Ble.Data;
using Darp.Ble.Implementation;
using Darp.Ble.Logger;

namespace Darp.Ble.WinRT;

/// <summary> Provides windows specific implementation of a ble device </summary>
public sealed class WinBleDevice(IObserver<(BleDevice, LogEvent)>? logger) : BleDevice(logger)
{
    /// <inheritdoc />
    protected override Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        Observer = new WinBleObserver(this, Logger);
        Central = new WinBleCentral(this, Logger);
        Peripheral = new WinBlePeripheral(this, Logger);
        return Task.FromResult(InitializeResult.Success);
    }

    /// <inheritdoc />
    public override string Name => "Windows";

    /// <inheritdoc />
    public override string Identifier => "Darp.Ble.WinRT";
}