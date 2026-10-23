using Darp.Ble.Data;

namespace Darp.Ble.Backends;

/// <summary> The driver of an opened adapter. The core wraps it in a <see cref="BleAdapter"/> and disposes it once </summary>
public interface IBleAdapterDriver : IAsyncDisposable
{
    /// <summary> The roles and features this adapter supports. Each role flag must match whether its role driver exists </summary>
    Capabilities Capabilities { get; }

    /// <summary> The observer role, or null if the adapter cannot scan </summary>
    IObserverDriver? Observer { get; }
}
