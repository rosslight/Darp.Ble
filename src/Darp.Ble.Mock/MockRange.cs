using Darp.Ble.Connections;

namespace Darp.Ble.Mock;

/// <summary>
/// Whether a simulated peripheral is in range of the other adapters in its air. A test creates it and passes it to
/// <see cref="MockBackendOptions.AddPeripheral"/>, e.g. to test what an app does when a device walks away and comes back.
/// It outlives opening and closing the mock adapter
/// </summary>
public sealed class MockRange
{
    // Guards the fields below: the test leaves and enters while the mock adapter opens and closes its peripherals
    private readonly Lock _lock = new();
    private readonly List<MockAdapterDriver> _devices = [];
    private bool _isInRange = true;

    /// <summary> Whether the peripheral is in range; true when created </summary>
    public bool IsInRange
    {
        get
        {
            lock (_lock)
                return _isInRange;
        }
    }

    /// <summary> The peripheral leaves range, as a device does that walks away or whose battery dies </summary>
    /// <remarks>
    /// Its links end as on a supervision timeout: both sides learn <see cref="DisconnectReason.ConnectionTimeout"/>.
    /// Until <see cref="Enter"/>, nobody hears its advertising and it hears nobody's, and connecting to it or from it fails
    /// with <see cref="BleConnectErrorKind.Unreachable"/>. Its advertising keeps running. While the mock adapter is closed,
    /// only the state changes
    /// </remarks>
    public void Leave()
    {
        MockAdapterDriver[] devices;
        lock (_lock)
        {
            _isInRange = false;
            devices = [.. _devices];
        }
        // Outside the lock: the links call into the core, which may connect again and ask whether the device is in range
        foreach (MockAdapterDriver device in devices)
            device.LoseLinks(DisconnectReason.ConnectionTimeout);
    }

    /// <summary> The peripheral is in range again: it is heard from its advertising's next event on and can connect </summary>
    public void Enter()
    {
        lock (_lock)
            _isInRange = true;
    }

    /// <summary> The peripheral's open driver joins the range until it is disposed </summary>
    internal void Join(MockAdapterDriver device)
    {
        lock (_lock)
            _devices.Add(device);
    }

    internal void Quit(MockAdapterDriver device)
    {
        lock (_lock)
            _devices.Remove(device);
    }
}
