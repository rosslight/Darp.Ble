using Darp.Ble.Data;
using Darp.Ble.Mock.Connections;

namespace Darp.Ble.Mock;

/// <summary> Configures the mock backend registered by <see cref="MockBleManagerBuilderExtensions.AddMock"/> </summary>
public sealed class MockBackendOptions
{
    private readonly List<MockPeripheral> _peripherals = [];
    private Func<TxPowerLevel, Rssi> _txPowerToRssi = txPower =>
        // A set without TX power is treated as sending with 0 dBm
        CalculateRssi(
            txPower is TxPowerLevel.NotAvailable ? (TxPowerLevel)0 : txPower,
            distance: 2,
            environmentalFactor: 2
        );

    /// <summary> The name of the mock adapter </summary>
    public string Name { get; set; } = "Mock";

    /// <summary> Whether the mock adapter's peripheral role serves the GATT service with Service Changed and Database Hash; true by default </summary>
    public bool IncludeGattService { get; set; } = true;

    /// <summary> Makes opening the mock adapter fail with this kind, e.g. to test how an app handles Bluetooth being off </summary>
    public BleOpenErrorKind? OpenFailure { get; set; }

    /// <summary> The signal strength the mock adapter's scans see for an advertiser's TX power </summary>
    /// <remarks> By default a device 2 m away in a typical room; a set without TX power sends with 0 dBm </remarks>
    public Func<TxPowerLevel, Rssi> TxPowerToRssi
    {
        get => _txPowerToRssi;
        set => _txPowerToRssi = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal IReadOnlyList<MockPeripheral> Peripherals => _peripherals;

    /// <summary> Adds a simulated peripheral that the mock adapter's scans see </summary>
    /// <param name="configure"> Sets the peripheral up when the mock adapter opens, with the peripheral's own open adapter, e.g. to
    /// start advertising; opening waits for it and fails with its exception. Work that keeps running, like a sensor that notifies,
    /// goes to <see cref="MockBackgroundWork.Run"/>. The peripheral lives as long as the mock adapter </param>
    /// <param name="name"> The name of the peripheral's adapter </param>
    /// <param name="includeGattService"> Whether the peripheral serves the GATT service; false simulates a device without it </param>
    /// <returns> These options </returns>
    public MockBackendOptions AddPeripheral(
        Func<BleAdapter, MockBackgroundWork, Task> configure,
        string name = "Mock peripheral",
        bool includeGattService = true
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        _peripherals.Add(new MockPeripheral(name, includeGattService, configure));
        return this;
    }

    /// <summary> Log-distance path loss: the TX power minus 10 · log10(distance) · factor </summary>
    private static Rssi CalculateRssi(TxPowerLevel txPower, double distance, double environmentalFactor)
    {
        double rssi = (double)txPower - (10.0d * Math.Log10(distance) * environmentalFactor);
        // A controller reports RSSI from -127 to +20 dBm
        return (Rssi)(sbyte)Math.Clamp(Math.Round(rssi), -127, 20);
    }
}
