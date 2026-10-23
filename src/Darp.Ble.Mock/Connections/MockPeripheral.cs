namespace Darp.Ble.Mock.Connections;

/// <summary> A simulated peripheral: its name and what it does when the mock adapter opens </summary>
internal sealed record MockPeripheral(string Name, Func<BleAdapter, MockBackgroundWork, Task> Configure);
