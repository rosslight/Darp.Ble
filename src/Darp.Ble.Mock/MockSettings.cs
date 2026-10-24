using Darp.Ble.Data;
using Darp.Ble.Mock.Connections;

namespace Darp.Ble.Mock;

/// <summary> What <see cref="MockBackendOptions"/> configured, fixed when the backend is registered </summary>
internal sealed record MockSettings(
    string Name,
    BleOpenErrorKind? OpenFailure,
    IReadOnlyList<MockPeripheral> Peripherals,
    Func<TxPowerLevel, Rssi> TxPowerToRssi,
    bool IncludeGattService
);
