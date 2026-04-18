using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Transport;

/// <summary>
/// Defines the transport used to exchange HCI packets with the controller.
/// </summary>
public interface ITransportLayer : IAsyncDisposable
{
    /// <summary>
    /// Queues an HCI packet for transmission to the controller.
    /// </summary>
    /// <param name="packet">The packet to transmit.</param>
    void Enqueue(IHciPacket packet);

    /// <summary>
    /// Initializes the transport and starts forwarding incoming packets.
    /// </summary>
    /// <param name="onReceived">Invoked for each packet received from the controller.</param>
    /// <param name="onError">Invoked when the transport encounters a fatal error.</param>
    /// <param name="cancellationToken">Cancels transport initialization.</param>
    /// <returns>A task that completes when initialization finishes.</returns>
    ValueTask InitializeAsync(
        Action<HciPacket> onReceived,
        Action<Exception> onError,
        CancellationToken cancellationToken
    );
}
