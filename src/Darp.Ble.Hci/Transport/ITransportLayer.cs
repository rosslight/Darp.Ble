using Darp.Ble.Hci.Package;

namespace Darp.Ble.Hci.Transport;

public interface ITransportLayer : IDisposable
{
    void Enqueue(IHciPacket packet);
    IObservable<IHciPacket> WhenReceived();
    void Initialize();
}