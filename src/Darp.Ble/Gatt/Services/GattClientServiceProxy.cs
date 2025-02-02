using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt.Services;

/// <summary> An abstract base for service proxies </summary>
/// <param name="service"> The underlying gatt client service </param>
[SuppressMessage(
    "Design",
    "CA1033:Interface methods should be callable by child types",
    Justification = "Child classes should only be wrappers and should not call any methods"
)]
public abstract class GattClientServiceProxy(IGattClientService service) : IGattClientService
{
    private readonly IGattClientService _service = service;

    /// <inheritdoc />
    public IBlePeripheral Peripheral => _service.Peripheral;

    /// <inheritdoc />
    public BleUuid Uuid => _service.Uuid;

    /// <inheritdoc />
    public GattServiceType Type => _service.Type;

    IReadOnlyCollection<IGattClientCharacteristic> IGattClientService.Characteristics =>
        _service.Characteristics;

    Task<IGattClientCharacteristic> IGattClientService.AddCharacteristicAsync(
        BleUuid uuid,
        GattProperty gattProperty,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite,
        CancellationToken cancellationToken
    ) => _service.AddCharacteristicAsync(uuid, gattProperty, onRead, onWrite, cancellationToken);
}
