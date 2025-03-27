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

internal sealed partial class HciHostBlePeripheral : BlePeripheral
{
    private readonly IDisposable _subscription;

    public HciHostBlePeripheral(HciHostBleDevice device, ILogger<HciHostBlePeripheral> logger)
        : base(device, logger)
    {
        Device = device;
        _subscription = device.Host.Subscribe(this);
    }

    public new HciHostBleDevice Device { get; }

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
                ServiceProvider.GetLogger<HciHostGattClientPeer>()
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
            ServiceProvider.GetLogger<HciHostGattClientService>()
        );
    }

    protected override void Dispose(bool disposing)
    {
        _subscription.Dispose();
        base.Dispose(disposing);
    }
}
