using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt;

/// <summary> Handles a central's write of a hosted characteristic </summary>
/// <param name="central"> The central that writes </param>
/// <param name="value"> The written value; a copy the handler may keep </param>
/// <param name="cancellationToken"> Cancelled when the central's connection ends </param>
/// <returns> The status to answer with, <see cref="GattProtocolStatus.Success"/> to accept the write </returns>
public delegate ValueTask<GattProtocolStatus> OnWriteHandler(
    ConnectedCentral central,
    ReadOnlyMemory<byte> value,
    CancellationToken cancellationToken
);
