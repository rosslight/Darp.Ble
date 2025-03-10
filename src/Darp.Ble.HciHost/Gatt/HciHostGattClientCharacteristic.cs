using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt;

internal sealed class HciHostGattClientCharacteristic(
    GattClientService clientService,
    GattProperty properties,
    IGattCharacteristicValue value,
    ILogger<HciHostGattClientCharacteristic> logger
) : GattClientCharacteristic(clientService, properties, value, logger)
{
    protected override void OnAddDescriptor(IGattCharacteristicValue value) { }

    protected override void NotifyCore(IGattClientPeer clientPeer, byte[] value)
    {
        throw new NotImplementedException();
    }

    protected override Task IndicateAsyncCore(
        IGattClientPeer clientPeer,
        byte[] value,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }
}
