using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT.Gatt.Server;

internal sealed class WinGattServerDescriptor(
    GattServerCharacteristic characteristic,
    GattDescriptor winDescriptor,
    ILogger<WinGattServerDescriptor> logger)
    : GattServerDescriptor(characteristic, BleUuid.FromGuid(winDescriptor.Uuid, inferType: true), logger);