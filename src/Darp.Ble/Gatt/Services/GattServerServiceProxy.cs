using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Gatt.Services;

/// <summary> An abstract base for service proxies </summary>
/// <param name="service"> The underlying gatt client service </param>
[SuppressMessage("Design", "CA1033:Interface methods should be callable by child types",
    Justification = "Child classes should only be wrappers and should not call any methods")]
public abstract class GattServerServiceProxy(IGattServerService service) : IGattServerService
{
    private readonly IGattServerService _service = service;

    /// <inheritdoc />
    public IGattServerPeer Peer => _service.Peer;
    /// <inheritdoc />
    public BleUuid Uuid => _service.Uuid;
    /// <inheritdoc />
    public GattServiceType Type => _service.Type;

    IReadOnlyCollection<IGattServerCharacteristic> IGattServerService.Characteristics => _service.Characteristics;
    Task IGattServerService.DiscoverCharacteristicsAsync(CancellationToken cancellationToken)
        => _service.DiscoverCharacteristicsAsync(cancellationToken);
    Task<IGattServerCharacteristic> IGattServerService.DiscoverCharacteristicAsync(BleUuid uuid, CancellationToken cancellationToken)
        => _service.DiscoverCharacteristicAsync(uuid, cancellationToken);
}