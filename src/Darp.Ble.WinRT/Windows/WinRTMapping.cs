using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Darp.Ble.WinRT;

/// <summary> Conversions between Windows' Bluetooth types and the library's </summary>
internal static class WinRTMapping
{
    public static BleAddress ToAddress(ulong address, BluetoothAddressType type) =>
        new(
            type switch
            {
                BluetoothAddressType.Public => BleAddressType.Public,
                BluetoothAddressType.Random => BleAddressType.Random,
                _ => BleAddressType.NotAvailable,
            },
            (UInt48)address
        );

    public static BluetoothAddressType ToWindows(BleAddressType type) =>
        type switch
        {
            BleAddressType.Public or BleAddressType.PublicIdentity => BluetoothAddressType.Public,
            BleAddressType.NotAvailable => BluetoothAddressType.Unspecified,
            _ => BluetoothAddressType.Random,
        };

    public static BleEventType ToEventType(BluetoothLEAdvertisementReceivedEventArgs report) =>
        report.AdvertisementType switch
        {
            BluetoothLEAdvertisementType.ConnectableUndirected => BleEventType.AdvInd,
            BluetoothLEAdvertisementType.ConnectableDirected => BleEventType.AdvDirectInd,
            BluetoothLEAdvertisementType.ScannableUndirected => BleEventType.AdvScanInd,
            BluetoothLEAdvertisementType.NonConnectableUndirected => BleEventType.AdvNonConnInd,
            // A legacy scan response answers ADV_IND (connectable) or ADV_SCAN_IND, both scannable
            BluetoothLEAdvertisementType.ScanResponse => BleEventType.Legacy
                | BleEventType.Scannable
                | BleEventType.ScanResponse
                | (report.IsConnectable ? BleEventType.Connectable : BleEventType.None),
            _ => (report.IsConnectable ? BleEventType.Connectable : BleEventType.None)
                | (report.IsScannable ? BleEventType.Scannable : BleEventType.None)
                | (report.IsDirected ? BleEventType.Directed : BleEventType.None)
                | (report.IsScanResponse ? BleEventType.ScanResponse : BleEventType.None),
        };

    public static GattCharacteristicProperties ToWindows(GattProperty properties) =>
        (GattCharacteristicProperties)(uint)properties;

    public static GattProperty ToProperties(GattCharacteristicProperties properties) =>
        (GattProperty)((uint)properties & 0xFF);
}
