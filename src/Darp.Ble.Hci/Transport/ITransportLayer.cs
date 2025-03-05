using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Transport;

/// <summary> An abstract implementation of a transport layer </summary>
public interface ITransportLayer : IDisposable
{
    /// <summary> Enqueue a new hci packet </summary>
    /// <param name="packet"> The packet to be enqueued </param>
    void Enqueue(IHciPacket packet);

    /// <summary> Initialize the transport layer </summary>
    void Initialize(Action<HciPacket> onReceived);
}
