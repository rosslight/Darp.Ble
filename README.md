# Darp.Ble

[![.NET](https://github.com/rosslight/Darp.Ble/actions/workflows/release-please.yml/badge.svg)](https://github.com/rosslight/Darp.Ble/actions/workflows/release-please.yml)
[![NuGet](https://img.shields.io/nuget/v/Darp.Ble.svg)](https://www.nuget.org/packages/Darp.Ble)
[![Downloads](https://img.shields.io/nuget/dt/Darp.Ble)](https://www.nuget.org/packages/Darp.Ble)
![License](https://img.shields.io/badge/license-AGPL--3.0%20or%20COMMERCIAL-blue)

Darp.Ble is an abstraction layer which aims to provide a simple, reactive way of using BLE with .Net while maintaining granular configuration options.

## Disclaimer

This package is under heavy development. Huge changes to the API are to be expected!

## Implementations

| Implementation   | Observer | Central | Broadcaster | Peripheral | Nuget                                                                                                                        |
|------------------|----------|---------|-------------|------------|------------------------------------------------------------------------------------------------------------------------------|
| Darp.Ble.WinRT   | X        | X       | X           | X          | [![Darp.Ble.WinRT](https://img.shields.io/nuget/v/Darp.Ble.WinRT.svg)](https://www.nuget.org/packages/Darp.Ble.WinRT/)       |
| Darp.Ble.Android | X        | X       |             |            | [![Darp.Ble.Android](https://img.shields.io/nuget/v/Darp.Ble.Android.svg)](https://www.nuget.org/packages/Darp.Ble.Android/) |
| Darp.Ble.HciHost | X        | X       | x           | x          | [![Darp.Ble.HciHost](https://img.shields.io/nuget/v/Darp.Ble.HciHost.svg)](https://www.nuget.org/packages/Darp.Ble.HciHost/) |
| Darp.Ble.iOS     |          |         |             |            | planned                                                                                                                      |
| Darp.Ble.Mac     |          |         |             |            | planned                                                                                                                      |
| Darp.Ble.BlueZ   |          |         |             |            | planned                                                                                                                      |
| Darp.Ble.Mock    | X        | X       |             |            | [![Darp.Ble.Mock](https://img.shields.io/nuget/v/Darp.Ble.Mock.svg)](https://www.nuget.org/packages/Darp.Ble.Mock/)          |

## Features

### Open an adapter

A `BleManager` lists the adapters of the backends you register. Opening one returns a ready `BleAdapter`, or throws a `BleOpenException` whose `Kind` says why (`NotAvailable`, `Disabled`, `MissingPermission`).
You own the adapter: disposing it closes it.

```csharp
var bleManager = new BleManager(builder => builder.AddMock());

IReadOnlyList<BleAdapterInfo> adapters = await bleManager.EnumerateAdaptersAsync();
await using BleAdapter adapter = await adapters[0].OpenAsync();
```

With dependency injection, `services.AddBleManager(builder => builder.AddMock())` registers the same manager and uses the container's `ILoggerFactory` and `TimeProvider`.
For unit tests, `await MockBle.OpenAdapterAsync()` opens a mock adapter in one line.

The [examples](examples) are single-file programs for one task each, e.g. `dotnet run examples/list-adapters.cs`.

### Scan

`adapter.Observer.ScanAsync()` scans while you enumerate it. Every call has its own queue, so a slow loop only delays itself, and all scans of an adapter share one radio scan.
Ending the loop, cancelling the token, or disposing the adapter ends the scan. The radio settings belong to the observer and apply to every scan.

```csharp
await adapter.Observer.ConfigureAsync(new BleScanParameters { ScanType = ScanType.Active });

await foreach (IGapAdvertisement advertisement in adapter.Observer.ScanAsync(cancellationToken).WhereService(0x180D))
    Console.WriteLine($"{advertisement.Address} {advertisement.Rssi}");
```

A queue holds 10,000 advertisements and drops the oldest when full. `ScanAsync(new QueueOptions(capacity, QueueFullMode.Fail), cancellationToken)` changes it for one scan, `builder.ConfigureQueues(...)` for all of them.

### Advertise

`adapter.Broadcaster.StartAdvertisingAsync(data, interval)` starts an advertising set right away; `CreateAdvertisingSetAsync(parameters, data)` creates one to start later. Disposing the set stops advertising.

```csharp
await using AdvertisingSet advertising = await adapter.Broadcaster.StartAdvertisingAsync(
    AdvertisingData.Empty.WithCompleteLocalName("Sensor"),
    interval: TimeSpan.FromMilliseconds(100));
```

In tests, `AddMock(mock => mock.AddPeripheral(...))` adds simulated peripherals that advertise into the mock adapter's scans.

### Central

If the device supports central mode, use the `Central` property. `.ConnectToPeripheral()`, publishes a peer peripheral once connected.

After that, service and characteristic discovery it is possible to read/write/subscribe.
- Notify: `.OnNotify()` gives back a connectable observable. You can register subscribers before actually subscribing using `.Connect()`.
- Write: `.WriteAsync()` allows you to write an array of bytes to the peerDevice

```csharp
var central = bleDevice.Central;

var peerDevice = await central
    .ConnectToPeripheral(new BleAddress(BleAddressType.Public, (UInt48)0xAABBCCDDEEFF))
    .FirstAsync();

var service = await peerDevice.DiscoverServiceAsync(0x1234);
var writeChar = await service.DiscoverCharacteristicAsync<Properties.Write>(0x5678);
var notifyChar = await service.DiscoverCharacteristicAsync<Properties.Notify>(0xABCD);

await using var disposableObs = await notifyChar.OnNotifyAsync();
var notifyTask = disposableObs.FirstAsync().ToTask();
await writeChar.WriteAsync([0x00, 0x02, 0x03, 0x04]);
var resultBytes = await notifyTask;
```

### Further Documentation

Additional documentation exists in form of the unit tests.

## Why use this library?

When deciding to write this library, we were unable to find a library meeting all of our requirements:
- C# language support
- Cross-platform including the ability to communicate with BLE dongles
- Granular configuration options if the platform supports it
- (Reactive interface)

## Setup Android development

You can install JDK and AndroidSDK using the [InstallAndroidDependencies](https://learn.microsoft.com/en-us/dotnet/android/getting-started/installation/dependencies#using-installandroiddependencies-target) target.
Optionally, you can set the environment variables to avoid having to set the directories in each build.

Note: The path `C:/work/...` is just an example

After that, restore the dotnet workloads to install the required android workload.

```powershell
dotnet build -t:InstallAndroidDependencies -f net10.0-android -p:AndroidSdkDirectory=c:\work\android-sdk -p:JavaSdkDirectory=c:\work\jdk -p:AcceptAndroidSdkLicenses=True
setx JAVA_HOME C:\work\jdk\
setx ANDROID_HOME C:\work\android-sdk\

# Restore android workload
dotnet workload restore
```

## About

Darp.Ble is a project by [rosslight GmbH](https://engineering.rosslight.de/), see also [darp.ble Product Page](https://engineering.rosslight.de/ble-suite)
