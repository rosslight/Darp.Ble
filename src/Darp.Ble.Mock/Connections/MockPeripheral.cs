namespace Darp.Ble.Mock.Connections;

/// <summary>
/// A simulated peripheral: its name, whether it serves the GATT service, what it does when the mock adapter opens, and
/// the range the test moves it in and out of
/// </summary>
internal sealed record MockPeripheral(
    string Name,
    bool IncludeGattService,
    Func<BleAdapter, MockBackgroundWork, Task> Configure,
    MockRange? Range
);
