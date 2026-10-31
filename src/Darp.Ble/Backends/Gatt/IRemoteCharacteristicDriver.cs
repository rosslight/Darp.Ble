using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Darp.Ble.Gatt;

namespace Darp.Ble.Backends;

/// <summary> A characteristic discovered on a peripheral. Belongs to its connection, which returns the same object for the same characteristic every time </summary>
/// <remarks> Its operations fail as <see cref="IConnectionDriver"/> describes </remarks>
[Experimental(Experiments.Backends)]
public interface IRemoteCharacteristicDriver
{
    /// <summary> The UUID of the characteristic </summary>
    BleUuid Uuid { get; }

    /// <summary> What the characteristic supports </summary>
    GattProperty Properties { get; }

    /// <summary> Reads the value; a long value takes several transactions </summary>
    /// <param name="request"> Reports each transaction; check <see cref="GattRequest.IsAbandoned"/> before the next one </param>
    /// <returns> The value </returns>
    /// <exception cref="GattException"> The peripheral answered with an ATT error </exception>
    Task<byte[]> ReadAsync(GattRequest request);

    /// <summary> Writes the value with a write request; a long value takes several transactions </summary>
    /// <param name="value"> The value; the core keeps it unchanged until the task completes </param>
    /// <param name="request"> Reports each transaction; once abandoned, the driver finishes the current one and cancels a prepared write </param>
    /// <returns> A task that completes once the peripheral accepted the value </returns>
    /// <exception cref="GattException"> The peripheral answered with an ATT error </exception>
    Task WriteAsync(ReadOnlyMemory<byte> value, GattRequest request);

    /// <summary> Writes the value with a write command, which the peripheral does not answer </summary>
    /// <param name="value"> The value; the driver takes what it needs before the task completes </param>
    /// <param name="cancellationToken"> The caller's token, honoured while waiting for buffer space; never once the backend accepted the packet </param>
    /// <param name="connectionClosed"> Cancelled when the connection ends, so a wait for buffer space that never frees up ends too </param>
    /// <returns> A task that completes once the backend accepted the packet; never only once the peripheral got it.
    /// It completes synchronously when there is buffer space </returns>
    ValueTask WriteWithoutResponseAsync(
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken,
        CancellationToken connectionClosed
    );

    /// <summary> Discovers the descriptors of the characteristic </summary>
    /// <param name="request"> Reports each transaction, e.g. each page; check <see cref="GattRequest.IsAbandoned"/> before the next one </param>
    /// <returns> The descriptors, in handle order; the same objects on every discovery </returns>
    Task<IReadOnlyList<IRemoteDescriptorDriver>> DiscoverDescriptorsAsync(GattRequest request);

    /// <summary> Writes the client characteristic configuration descriptor to turn notifications on or off </summary>
    /// <param name="enabled"> Whether notifications are on afterwards </param>
    /// <param name="request"> Reports the transaction </param>
    /// <returns> A task that completes once the peripheral accepted the change </returns>
    /// <exception cref="GattException"> The peripheral answered with an ATT error </exception>
    Task SetNotificationsAsync(bool enabled, GattRequest request);
}
