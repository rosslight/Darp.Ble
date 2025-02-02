using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Gatt;

internal sealed class MockGattServerDescriptor(
    MockGattServerCharacteristic characteristic,
    BleUuid uuid,
    MockGattClientDescriptor mockDescriptor,
    ILogger<MockGattServerDescriptor> logger) : GattServerDescriptor(characteristic, uuid, logger);