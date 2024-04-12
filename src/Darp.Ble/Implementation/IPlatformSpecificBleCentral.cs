using System.Collections.ObjectModel;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Implementation;

/// <summary> The ble central implementation </summary>
public interface IPlatformSpecificBleCentral
{
    /// <summary> Connect to remote peripheral </summary>
    /// <param name="address"> The address to be connected to </param>
    /// <param name="connectionParameters"> The connection parameters to be used </param>
    /// <param name="scanParameters"> The scan parameters to be used for initial discovery </param>
    IObservable<GattServerDevice> ConnectToPeripheral(BleAddress address, BleConnectionParameters connectionParameters, BleScanParameters scanParameters);
}