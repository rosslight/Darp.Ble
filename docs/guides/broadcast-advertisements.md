# Broadcast advertisements

Broadcasting uses the `Broadcaster` role to publish GAP advertising data. Use it when your application should be discoverable or should emit local data without a connection.

## Requirements

- The initialized device must have `Capabilities.Broadcaster`.
- Connectable advertising usually belongs with [Accept connections](accept-connections/), because the local peripheral has to serve the GATT database after a central connects.
- [HciHost](../platforms/hci-host/) and [Mock](../platforms/mock/) are the main backends for this workflow today.

## Create advertising data

```csharp
using Darp.Ble.Gap;

AdvertisingData data = AdvertisingData.Empty
    .WithCompleteLocalName(device.Name ?? "Darp.Ble");
```

Keep advertising data small. Put discoverability hints in the advertisement and move detailed data into GATT.

## Start and stop advertising

```csharp
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;

IAdvertisingSet set = await device.Broadcaster.CreateAdvertisingSetAsync(
    type: BleEventType.AdvInd,
    data: data,
    interval: ScanTiming.Ms1000);

await using IAsyncDisposable advertising = await set.StartAdvertisingAsync();
```

Disposing `advertising` stops the advertising set.

## Update advertising data

Advertising data can be updated through the advertising set.

```csharp
await set.SetAdvertisingDataAsync(
    data.WithCompleteLocalName("Darp.Ble sensor"));
```

## Backend limits

- HciHost depends on controller support for advertising set count and maximum advertising data length.
- Mock advertising is in-process and intended for tests or local development.
- WinRT advertising support depends on Windows adapter capability flags.
