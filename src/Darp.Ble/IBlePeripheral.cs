using System.Reactive;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Database;

namespace Darp.Ble;

/// <summary> The ble peripheral </summary>
public interface IBlePeripheral
{
    /// <summary> The ble device </summary>
    IBleDevice Device { get; }

    /// <summary> The gatt database </summary>
    internal IGattDatabase GattDatabase { get; }

    /// <summary> A list of all centrals this peripheral is connected to </summary>
    IReadOnlyDictionary<BleAddress, IGattClientPeer> PeerDevices { get; }

    /// <summary> A collection of all added services </summary>
    IReadonlyAttributeCollection<IGattClientService> Services { get; }

    /// <summary> An observable which fires when a new GattClient was connected </summary>
    IObservable<IGattClientPeer> WhenConnected { get; }

    /// <summary> An observable which fires when a GattClient disconnected </summary>
    IObservable<IGattClientPeer> WhenDisconnected { get; }

    /// <summary> An observable which fires when a service (or an attribute inside a service) has changed </summary>
    IObservable<Unit> WhenServiceChanged { get; }

    /// <summary> Add a new service to this peripheral </summary>
    /// <param name="uuid"> The uuid of the service to be added </param>
    /// <param name="isPrimary"> True, if the service is a primary service; False, if secondary </param>
    /// <returns> The newly added service </returns>
    IGattClientService AddService(BleUuid uuid, bool isPrimary = true);
}
