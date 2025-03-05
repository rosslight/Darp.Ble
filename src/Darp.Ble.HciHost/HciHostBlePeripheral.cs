using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.HciHost.Gatt;
using Darp.Ble.Implementation;
using Darp.Utils.Messaging;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost;

internal sealed partial class HciHostBlePeripheral(HciHostBleDevice device, ILogger<HciHostBlePeripheral> logger)
    : BlePeripheral(device, logger)
{
    private readonly Hci.HciHost _host = device.Host;

    public new HciHostBleDevice Device { get; } = device;

    [MessageSink]
    private void OnHciConnectionCompleteEvent(HciLeEnhancedConnectionCompleteV1Event connectionCompleteEvent)
    {
        if (connectionCompleteEvent.Status is not HciCommandStatus.Success)
        {
            Logger.LogWarning(
                "Received connection request but is failed with status {Status}",
                connectionCompleteEvent.Status
            );
            return;
        }
        var peerDeviceAddress = new BleAddress(
            (BleAddressType)connectionCompleteEvent.PeerAddressType,
            (UInt48)(ulong)connectionCompleteEvent.PeerAddress
        );

        Logger.LogDebug(
            "Connection 0x{Handle:X4} with {PeerAddress} completed",
            connectionCompleteEvent.ConnectionHandle,
            peerDeviceAddress
        );
        if (!PeerDevices.TryGetValue(peerDeviceAddress, out IGattClientPeer? peerDevice))
        {
            peerDevice = new HciHostGattClientPeer(
                this,
                peerDeviceAddress,
                connectionCompleteEvent.ConnectionHandle,
                LoggerFactory.CreateLogger<HciHostGattClientPeer>()
            );
            OnConnectedCentral(peerDevice);
        }
    }

    protected override GattClientService AddServiceCore(BleUuid uuid, bool isPrimary)
    {
        return new HciHostGattClientService(
            this,
            uuid,
            isPrimary ? GattServiceType.Primary : GattServiceType.Secondary,
            LoggerFactory.CreateLogger<HciHostGattClientService>()
        );
    }
}
