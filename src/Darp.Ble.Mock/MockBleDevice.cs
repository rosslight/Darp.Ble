using Darp.Ble.Data;
using Darp.Ble.Logger;

namespace Darp.Ble.Mock;

/// <summary> Provides windows specific implementation of a ble device </summary>
public sealed class MockBleDevice(
    Func<MockBleBroadcaster, IBlePeripheral, Task> configure,
    IObserver<(BleDevice, LogEvent)>? logger) : BleDevice(logger)
{
    private readonly Func<MockBleBroadcaster, IBlePeripheral, Task> _configure = configure;

    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        var broadcaster = new MockBleBroadcaster();
        var peripheral = new MockBlePeripheral(this, broadcaster, Logger);
        Observer = new MockBleObserver(this, broadcaster, Logger);
        Central = new MockBleCentral(this, peripheral, Logger);
        await _configure.Invoke(broadcaster, peripheral);
        return InitializeResult.Success;
    }

    /// <inheritdoc />
    public override string Name => "Mock";

    /// <inheritdoc />
    public override string Identifier => "Darp.Ble.Mock";
}