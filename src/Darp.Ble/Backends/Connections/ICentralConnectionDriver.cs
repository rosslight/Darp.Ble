using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;

namespace Darp.Ble.Backends;

/// <summary> A central connected to the adapter's peripheral role. Belongs to its peripheral driver, which reports it through <see cref="IGattServerSink"/> </summary>
[Experimental(Experiments.Backends)]
public interface ICentralConnectionDriver
{
    /// <summary> The address of the central </summary>
    BleAddress Address { get; }
}
