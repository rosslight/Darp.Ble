# Testing with mock

Darp.Ble.Mock gives tests an in-process BLE environment. The primary mock device scans and connects; configured mock peripherals advertise and serve GATT data.

## Requirements

- Add `Darp.Ble.Mock`.
- Use `AddMock()` when creating the manager.
- Configure mock peripherals before calling `CreateManager()`.

## Add a mock backend

```csharp
using Darp.Ble;
using Darp.Ble.Mock;

var manager = new BleManagerBuilder()
    .AddMock()
    .CreateManager();
```

## Add a mock peripheral

Assign stable addresses in tests so the central can connect deterministically.

```csharp
using Darp.Ble;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Gatt.Services;
using Darp.Ble.Mock;

BleAddress address = BleAddress.NewRandomStaticAddress();

var manager = new BleManagerBuilder()
    .AddMock(factory =>
    {
        factory.AddPeripheral(async device =>
        {
            await device.SetRandomAddressAsync(address);
            device.Peripheral.AddBatteryService(initialBatteryLevel: 80);

            await device.Broadcaster.StartAdvertisingAsync(
                data: AdvertisingData.Empty
                    .WithCompleteLocalName("Test peripheral")
                    .WithCompleteListOfServiceUuids(device.Peripheral));
        }, name: "Test peripheral");
    })
    .CreateManager();
```

## Exercise the flow under test

The device returned by `BleManager` is the primary mock device. Initialize it, then use the same scan and central-connection APIs as production code.

```csharp
using System.Reactive.Linq;
using Darp.Ble;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;

IBleDevice device = manager.EnumerateDevices().First();
await device.InitializeAsync();

await using IGattServerPeer peer = await device.Observer
    .OnAdvertisement()
    .WhereConnectable()
    .ConnectToPeripheral()
    .FirstAsync();
```

## Use mock helpers for GATT tests

`Darp.Ble.Mock.Testing` includes helpers for tests that only need a connected GATT peer or service.

```csharp
using Darp.Ble.Gatt.Server;
using Darp.Ble.Gatt.Services;
using Darp.Ble.Mock.Testing;

IGattServerPeer peer = await MockHelpers.CreateMockedPeerDevice(peripheral =>
{
    peripheral.AddBatteryService(initialBatteryLevel: 80);
});
```

## Limits

- Mock behavior is local and deterministic; it does not prove OS Bluetooth behavior.
- `AddCentral(...)` is currently a placeholder and does not add simulated central behavior.
- Use [Platforms](../platforms/) when a test needs real backend setup details.
- Use [Scan advertisements](scan-advertisements/) and [Connect as a central](connect-as-central/) for the production workflows that this backend exercises.
