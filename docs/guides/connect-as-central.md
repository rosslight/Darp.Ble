# Connect as a central

The `Central` role initiates a GATT connection to a remote peripheral. The usual flow is scan, filter for a connectable advertisement, connect, discover GATT, read or write characteristics, subscribe to notifications, then dispose the peer.

## Requirements

- The initialized device must have `Capabilities.Central`.
- The target advertisement must include `BleEventType.Connectable`.
- [Android](../platforms/android/) does not currently initialize the central role.

## Connect from a scan result

```csharp
using System.Reactive.Linq;
using Darp.Ble;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Server;

await device.Observer.StartObservingAsync();

await using IGattServerPeer peer = await device.Observer
    .OnAdvertisement()
    .WhereConnectable()
    .ConnectToPeripheral()
    .FirstAsync();
```

`ConnectToPeripheral()` on an advertisement uses the central role of the device that observed the advertisement.

## Connect by address

Use this when the address is already known and you do not need to keep the advertisement object.

```csharp
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;

BleAddress address = new(BleAddressType.RandomStatic, (UInt48)0xF1F2F3F4F5F6);

await using IGattServerPeer peer = await device.Central
    .ConnectToPeripheral(address)
    .FirstAsync();
```

## Discover and use GATT

Discover the service and characteristic you need, then read or write bytes.

```csharp
using Darp.Ble.Gatt.Server;

IGattServerService service = await peer.DiscoverServiceAsync(0x180F);
IGattServerCharacteristic characteristic = await service.DiscoverCharacteristicAsync(0x2A19);

byte[] value = await characteristic.ReadAsync(CancellationToken.None);
await characteristic.WriteAsync([0x01, 0x02], CancellationToken.None);
```

Use typed service helpers, such as `DiscoverBatteryServiceAsync()`, when the repository already provides a contract for the service.

## Subscribe to notifications

Notifications are updates from a remote GATT characteristic. The characteristic must expose the `Notify` property.

```csharp
using Darp.Ble.Gatt.Server;
using static Darp.Ble.Gatt.Properties;

IGattServerCharacteristic<Notify> characteristic =
    await service.DiscoverCharacteristicAsync<Notify>(0x2A19);

await using IAsyncDisposable subscription = await characteristic.OnNotifyAsync(bytes =>
{
    // Handle the notification payload.
});
```

Dispose `subscription` to unsubscribe.

Use the observable overload when the rest of the workflow is already reactive.

```csharp
using System.Reactive.Linq;
using Darp.Ble.Gatt.Server;

await using IDisposableObservable<byte[]> notifications =
    await characteristic.OnNotifyAsync();

using IDisposable sink = notifications.Subscribe(bytes =>
{
    // Handle each notification.
});
```

Server-side notification code belongs with the local service setup. See [Accept connections](accept-connections/) for notifying connected centrals from a local peripheral.

## Dispose the connection

`IGattServerPeer` is async-disposable. Prefer `await using` so the connection is closed even when later GATT work fails.

## Backend limits

- [HciHost](../platforms/hci-host/) and [Mock](../platforms/mock/) provide central support.
- [WinRT](../platforms/winrt/) depends on Windows adapter capabilities.
- [Android](../platforms/android/) is scanner-focused today.
- Notifications stop when the returned subscription is disposed or the peer disconnects.
