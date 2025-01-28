using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble;

/// <summary> The ble peripheral </summary>
public interface IBlePeripheral
{
    /// <summary> The ble device </summary>
    IBleDevice Device { get; }

    /// <summary> A list of all centrals this peripheral is connected to </summary>
    IReadOnlyDictionary<BleAddress, IGattClientPeer> PeerDevices { get; }

    /// <summary> A collection of all added services </summary>
    IReadOnlyCollection<IGattClientService> Services { get; }

    /// <summary> An observable which fires when a new GattClient was connected </summary>
    IObservable<IGattClientPeer> WhenConnected { get; }
    /// <summary> An observable which fires when a GattClient disconnected </summary>
    IObservable<IGattClientPeer> WhenDisconnected { get; }

    /// <summary> Add a new service to this peripheral </summary>
    /// <param name="uuid"> The uuid of the service to be added </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> The newly added service </returns>
    Task<IGattClientService> AddServiceAsync(BleUuid uuid, CancellationToken cancellationToken = default);
}