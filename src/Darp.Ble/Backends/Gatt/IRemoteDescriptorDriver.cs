using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Darp.Ble.Gatt;

namespace Darp.Ble.Backends;

/// <summary> A descriptor discovered on a peripheral. Belongs to its connection </summary>
/// <remarks> Its operations fail as <see cref="IConnectionDriver"/> describes </remarks>
[Experimental(Experiments.Backends)]
public interface IRemoteDescriptorDriver
{
    /// <summary> The UUID of the descriptor </summary>
    BleUuid Uuid { get; }

    /// <summary> Reads the value </summary>
    /// <param name="request"> Reports each transaction </param>
    /// <returns> The value </returns>
    /// <exception cref="GattException"> The peripheral answered with an ATT error </exception>
    Task<byte[]> ReadAsync(GattRequest request);

    /// <summary> Writes the value </summary>
    /// <param name="value"> The value; the core keeps it unchanged until the task completes </param>
    /// <param name="request"> Reports each transaction </param>
    /// <returns> A task that completes once the peripheral accepted the value </returns>
    /// <exception cref="GattException"> The peripheral answered with an ATT error </exception>
    Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request);
}
