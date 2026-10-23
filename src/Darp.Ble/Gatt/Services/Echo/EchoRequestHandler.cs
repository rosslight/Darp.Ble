using Darp.Ble.Connections;

namespace Darp.Ble.Gatt.Services;

/// <summary> Answers a request written to a hosted echo service </summary>
/// <param name="central"> The central that wrote the request; it gets the reply </param>
/// <param name="request"> The request; a copy the handler may keep </param>
/// <param name="cancellationToken"> Cancelled when the central's connection ends </param>
/// <returns> The reply, notified to that central </returns>
public delegate ValueTask<byte[]> EchoRequestHandler(
    ConnectedCentral central,
    ReadOnlyMemory<byte> request,
    CancellationToken cancellationToken
);
