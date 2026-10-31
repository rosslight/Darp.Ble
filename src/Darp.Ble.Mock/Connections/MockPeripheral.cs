namespace Darp.Ble.Mock.Connections;

/// <summary> A simulated peripheral: its name, whether it serves the GATT service, and what it does when the mock adapter opens </summary>
internal sealed record MockPeripheral(
    string Name,
    bool IncludeGattService,
    Func<BleAdapter, MockBackgroundWork, Task> Configure
);
