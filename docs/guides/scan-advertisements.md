# Scan advertisements

Scanning uses the `Observer` role to receive BLE advertisements. Use it to discover nearby devices, filter by advertisement data, and feed connectable advertisements into [Connect as a central](connect-as-central/).

## Requirements

- The initialized device must have `Capabilities.Observer`.
- [Android](../platforms/android/) also needs scan permission and an available `BluetoothLeScanner`.
- Active scans can receive scan responses; passive scans only receive the advertising packet.

## Configure scan parameters

Set scan parameters before starting observation:

```csharp
using Darp.Ble.Data;

device.Observer.Configure(new BleObservationParameters
{
    ScanType = ScanType.Active,
    ScanInterval = ScanTiming.Ms100,
    ScanWindow = ScanTiming.Ms100,
});
```

## Start and stop scanning

Register the subscription, start scanning, then stop scanning when the workflow is done.

```csharp
using System.Reactive.Linq;
using Darp.Ble;
using Darp.Ble.Gap;

using IDisposable subscription = device.Observer
    .OnAdvertisement()
    .Subscribe(advertisement =>
    {
        // Read advertisement.Address, advertisement.Rssi, advertisement.Data, ...
    });

await device.Observer.StartObservingAsync();

// Keep the scan alive while your application needs results.

await device.Observer.StopObservingAsync();
```

## Filter results

The advertisement stream is an `IObservable<IGapAdvertisement>`, so use reactive operators and Darp.Ble's advertisement filters.

```csharp
using Darp.Ble;
using Darp.Ble.Gap;

IObservable<IGapAdvertisement> matches = device.Observer
    .OnAdvertisement()
    .CombineWithScanResponse()
    .WhereConnectable()
    .WhereService(0x180F);
```

`CombineWithScanResponse()` is useful for active scans when the data you need may be split between the advertisement and the scan response.

## Failure cases

- Accessing `device.Observer` before successful initialization throws.
- Accessing `device.Observer` without observer capability throws.
- Starting observation can fail with `BleObservationStartException`.
- On Android, location services and scan permission affect whether scan results are delivered.
