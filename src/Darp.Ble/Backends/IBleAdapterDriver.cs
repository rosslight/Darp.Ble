using Darp.Ble.Data;

namespace Darp.Ble.Backends;

/// <summary> The driver of an opened adapter. The core wraps it in a <see cref="BleAdapter"/> and disposes it once </summary>
public interface IBleAdapterDriver : IAsyncDisposable
{
    /// <summary> The features beyond the roles; the core adds the roles of the role drivers </summary>
    Capabilities Features { get; }

    /// <summary> The observer role, or null if the adapter cannot scan </summary>
    IObserverDriver? Observer { get; }
}
