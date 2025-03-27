using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Att;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload.Att;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt;

internal sealed class HciHostGattClientCharacteristic(
    GattClientService clientService,
    GattProperty properties,
    IGattCharacteristicValue value,
    ILogger<HciHostGattClientCharacteristic> logger
) : GattClientCharacteristic(clientService, properties, value, logger)
{
    protected override async ValueTask NotifyAsyncCore(IGattClientPeer clientPeer, byte[] value)
    {
        if (clientPeer is not HciHostGattClientPeer hciHostClientPeer)
            return;
        if (!Descriptors.TryGet(DescriptorDeclaration.ClientCharacteristicConfiguration.Uuid, out var cccd))
            throw new NotSupportedException();
        byte[] cccdValue = await cccd.ReadValueAsync(clientPeer, ServiceProvider).ConfigureAwait(false);
        if ((cccdValue[0] & 0b1) != 0b1)
            return;
        hciHostClientPeer.EnqueueGattPacket(
            new AttHandleValueNtf { Handle = Value.Handle, Value = value },
            activity: null
        );
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
