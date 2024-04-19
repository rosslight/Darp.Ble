using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble;

/// <summary> The ble central </summary>
public interface IBleCentral : IAsyncDisposable
{
    /// <summary> The ble device </summary>
    IBleDevice Device { get; }
    /// <summary> A list of all peripherals this central is connected to </summary>
    IReadOnlyCollection<IGattServerPeer> PeerDevices { get; }

    /// <summary> Connect to remote peripheral </summary>
    /// <param name="address"> The address to be connected to </param>
    /// <param name="connectionParameters"> The connection parameters to be used </param>
    /// <param name="scanParameters"> The scan parameters to be used for initial discovery </param>
    /// <returns> An observable notifying when a gatt server was connected </returns>
    IObservable<IGattServerPeer> ConnectToPeripheral(BleAddress address,
        BleConnectionParameters? connectionParameters = null,
        BleScanParameters? scanParameters = null);
}