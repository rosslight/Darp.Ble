using Darp.Ble.Data;

namespace Darp.Ble.Backends;

/// <summary> A central connected to the adapter's peripheral role. Belongs to its peripheral driver, which reports it through <see cref="IGattServerSink"/> </summary>
public interface ICentralConnectionDriver
{
    /// <summary> The address of the central </summary>
    BleAddress Address { get; }
}
