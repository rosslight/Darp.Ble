using Darp.Ble.Data;

namespace Darp.Ble.Backends;

/// <summary> The driver of an opened adapter. The core wraps it in a <see cref="BleAdapter"/> and disposes it once </summary>
public interface IBleAdapterDriver : IAsyncDisposable
{
    /// <summary> The roles this adapter supports </summary>
    Capabilities Capabilities { get; }
}
