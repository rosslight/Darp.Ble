# Accept connections

Accepting connections uses the `Peripheral` role. A local peripheral hosts GATT services, tracks connected centrals, and can notify subscribed centrals when values change.

## Requirements

- The initialized device must have `Capabilities.Peripheral`.
- The device also needs `Capabilities.Broadcaster` when it should advertise itself as discoverable.
- [HciHost](../platforms/hci-host/) and [Mock](../platforms/mock/) are the main backends for this workflow today.

## Add local services

Add services to `device.Peripheral` before advertising them.

```csharp
using Darp.Ble.Gatt.Services;

var batteryService = device.Peripheral.AddBatteryService(initialBatteryLevel: 50);
```

Use the built-in service helpers when they fit. For custom services, add a service to `device.Peripheral`, then add characteristics to that service.

## Expose service UUIDs

Put the local service UUIDs into advertising data so centrals can discover the peripheral before connecting.

```csharp
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;

AdvertisingData data = AdvertisingData.Empty
    .WithCompleteLocalName(device.Name ?? "Darp.Ble")
    .WithCompleteListOfServiceUuids(device.Peripheral);

await using IAsyncDisposable advertising = await device.Broadcaster.StartAdvertisingAsync(
    type: BleEventType.AdvInd,
    data: data,
    interval: ScanTiming.Ms1000);
```

See [Broadcast advertisements](broadcast-advertisements/) for advertising-set details.

## Track connected centrals

Subscribe to connection events when the application needs per-central state.

```csharp
using System.Reactive.Linq;
using Darp.Ble.Gatt.Client;

using IDisposable connected = device.Peripheral.WhenConnected.Subscribe((IGattClientPeer peer) =>
{
    // peer.Address identifies the connected central.
});

using IDisposable disconnected = device.Peripheral.WhenDisconnected.Subscribe((IGattClientPeer peer) =>
{
    // Clean up state for peer.Address.
});
```

`device.Peripheral.PeerDevices` contains the currently connected centrals.

## Notify connected centrals

Notify from the local characteristic after a central has subscribed.

```csharp
using Darp.Ble.Gatt;

await batteryService.BatteryLevel.NotifyAllAsync((byte)75);
```

Use `NotifyAsync(peer, value)` instead of `NotifyAllAsync(...)` when only one connected central should receive the update.

## Backend limits

- A central must connect and subscribe before notifications are delivered.
- Mock peripheral behavior is local and deterministic; it is useful for tests, not OS Bluetooth validation.
- WinRT peripheral support depends on Windows adapter capability flags.
