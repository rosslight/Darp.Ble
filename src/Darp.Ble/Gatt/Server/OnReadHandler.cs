using Darp.Ble.Connections;

namespace Darp.Ble.Gatt;

/// <summary> Answers a central's read of a hosted characteristic or descriptor </summary>
/// <remarks> Handlers run on thread-pool threads, one at a time per central and concurrently across centrals </remarks>
/// <param name="central"> The central that reads </param>
/// <param name="cancellationToken"> Cancelled when the central's connection ends </param>
/// <returns> The value; a central reads at most 512 bytes of it, the longest value of an attribute.
/// Throw a <see cref="GattException"/> with an ATT status to answer with that error </returns>
public delegate ValueTask<byte[]> OnReadHandler(ConnectedCentral central, CancellationToken cancellationToken);

/// <summary> Answers a central's read of a typed hosted characteristic </summary>
/// <remarks> Handlers run on thread-pool threads, one at a time per central and concurrently across centrals </remarks>
/// <param name="central"> The central that reads </param>
/// <param name="cancellationToken"> Cancelled when the central's connection ends </param>
/// <typeparam name="T"> The type of the value; the declaration encodes it </typeparam>
/// <returns> The value. Throw a <see cref="GattException"/> with an ATT status to answer with that error </returns>
public delegate ValueTask<T> OnReadHandler<T>(ConnectedCentral central, CancellationToken cancellationToken);
