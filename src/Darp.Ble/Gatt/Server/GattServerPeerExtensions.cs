using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Server;

/// <summary> Extensions of <see cref="IGattServerPeer"/> </summary>
public static class GattServerPeerExtensions
{
    /// <inheritdoc cref="IGattServerPeer.DiscoverServiceAsync"/>
    public static Task<IGattServerService> DiscoverServiceAsync(this IGattServerPeer peer,
        ushort uuid,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peer);
        return peer.DiscoverServiceAsync(new BleUuid(uuid), cancellationToken);
    }
}