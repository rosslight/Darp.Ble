using Darp.Ble.Connections;

namespace Darp.Ble.Gatt;

/// <summary> Handles a central's write of a hosted characteristic or descriptor, with or without response </summary>
/// <remarks>
/// Handlers run on thread-pool threads, one at a time per central and concurrently across centrals.
/// A write request is answered with the status, or with the ATT status of a <see cref="GattException"/> the handler
/// throws; any other exception is logged and answers <see cref="GattProtocolStatus.UnlikelyError"/>.
/// A write without response gets no answer, so its status is ignored
/// </remarks>
/// <param name="central"> The central that writes </param>
/// <param name="value"> The written value; a copy the handler may keep </param>
/// <param name="cancellationToken"> Cancelled when the central's connection ends </param>
/// <returns> The status to answer a write request with, <see cref="GattProtocolStatus.Success"/> to accept it </returns>
public delegate ValueTask<GattProtocolStatus> OnWriteHandler(
    ConnectedCentral central,
    ReadOnlyMemory<byte> value,
    CancellationToken cancellationToken
);

/// <summary> Handles a central's write of a typed hosted characteristic, with or without response </summary>
/// <remarks>
/// Handlers run on thread-pool threads, one at a time per central and concurrently across centrals.
/// A write request is answered with the status, or with the ATT status of a <see cref="GattException"/> the handler
/// throws; any other exception is logged and answers <see cref="GattProtocolStatus.UnlikelyError"/>.
/// A write without response gets no answer, so its status is ignored
/// </remarks>
/// <param name="central"> The central that writes </param>
/// <param name="value"> The written value, decoded by the declaration </param>
/// <param name="cancellationToken"> Cancelled when the central's connection ends </param>
/// <typeparam name="T"> The type of the value </typeparam>
/// <returns> The status to answer a write request with, <see cref="GattProtocolStatus.Success"/> to accept it </returns>
public delegate ValueTask<GattProtocolStatus> OnWriteHandler<T>(
    ConnectedCentral central,
    T value,
    CancellationToken cancellationToken
);
