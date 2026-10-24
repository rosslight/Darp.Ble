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
| Darp.Ble.Android | X        |         |             |            | [![Darp.Ble.Android](https://img.shields.io/nuget/v/Darp.Ble.Android.svg)](https://www.nuget.org/packages/Darp.Ble.Android/) |
| Darp.Ble.HciHost | X        | X       | X           | X          | [![Darp.Ble.HciHost](https://img.shields.io/nuget/v/Darp.Ble.HciHost.svg)](https://www.nuget.org/packages/Darp.Ble.HciHost/) |
| Darp.Ble.iOS     |          |         |             |            | planned                                                                                                                      |
| Darp.Ble.Mac     |          |         |             |            | planned                                                                                                                      |
| Darp.Ble.BlueZ   |          |         |             |            | planned                                                                                                                      |
| Darp.Ble.Mock    | X        | X       | X           | X          | [![Darp.Ble.Mock](https://img.shields.io/nuget/v/Darp.Ble.Mock.svg)](https://www.nuget.org/packages/Darp.Ble.Mock/)          |

## Features

### Open an adapter

A `BleManager` lists the adapters of the backends you register. Opening one returns a ready `BleAdapter`, or throws a `BleOpenException` whose `Kind` says why (`NotAvailable`, `Disabled`, `MissingPermission`).
You own the adapter: disposing it closes it.

```csharp
var bleManager = new BleManager(builder => builder.AddMock());

IReadOnlyList<BleAdapterInfo> adapters = await bleManager.EnumerateAdaptersAsync();
await using BleAdapter adapter = await adapters[0].OpenAsync();
```

`builder.AddSerialHciHost()` adds an adapter for every HCI dongle on a USB serial port (the nRF52840 dongle by default, more through `DeviceNameMapping`), and `builder.AddSerialHciHost("COM3")` the one on that port.
`builder.AddWinRT()` adds Windows' Bluetooth adapter and `builder.AddAndroid()` Android's; see [Backends](#backends) for what each one supports.
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

### Connect

`adapter.Central.ConnectAsync(address)` returns a `ConnectedPeripheral` once the link is up and the MTU is agreed, or throws a `BleConnectException` whose `Kind` says why (`Unreachable`, `TimedOut`, `Rejected`, `Busy`).
You own the connection: disposing it disconnects. `Disconnected` completes once with the reason, also when the peripheral disconnects first.
The adapter's scans pause while it connects and continue afterwards.

```csharp
await using ConnectedPeripheral connection = await adapter.Central.ConnectAsync(advertisement.Address);

RemoteGattService service = await connection.Gatt.DiscoverServiceAsync(0x180D);
foreach (RemoteGattCharacteristic characteristic in await service.DiscoverCharacteristicsAsync())
    Console.WriteLine($"{characteristic.Uuid} {characteristic.Properties}");
```

Requests of a connection run one at a time, in call order. A request whose peer does not answer within 30 seconds ends the connection with `DisconnectReason.AttTimeout`, and requests of an ended connection throw a `BleDisconnectedException` with the same `DisconnectInfo`.

### Read, write and notifications

A characteristic declaration describes what a characteristic supports; `DiscoverCharacteristicAsync(declaration)` returns a characteristic that only offers those operations, typed when the declaration has a value type.
`WriteWithoutResponseAsync` completes once the backend accepted the packet, so a loop of awaited writes is paced to what the radio can send.

```csharp
var setpoint = CharacteristicDeclaration.Create<ushort, Properties.Read, Properties.Write>(0xFFF1);
var remote = await service.DiscoverCharacteristicAsync(setpoint);
await remote.WriteAsync((ushort)22);
ushort value = await remote.ReadAsync();
```

`Subscribe()` starts a stream without any I/O, so it can exist before notifications are on; `EnableNotificationsAsync()` turns them on once per connection.
Each stream has its own queue. A stream ends with its connection: normally when you disconnected, with a `BleDisconnectedException` when the link was lost.

```csharp
using NotificationStream<byte[]> stream = measurement.Subscribe();
await measurement.EnableNotificationsAsync();
await foreach (byte[] value in stream)
    Console.WriteLine(Convert.ToHexString(value));
```

### Host a service

`adapter.Peripheral.Gatt.AddServiceAsync(builder)` publishes a complete service. `ConnectedCentrals` lists the centrals that are connected.
Read and write handlers get the `ConnectedCentral` that asked and a token that is cancelled when its connection ends; the handlers of one central run one at a time.
Declarations work on this side too: a typed declaration decodes what centrals write and encodes what handlers return. `NotifyAsync(central, value)` notifies one central, `NotifyAllAsync(value)` every central that turned notifications on.
`central.GetOrAddState(() => new Session())` keeps state for one connection and disposes it once the connection ended and its handlers returned.
A connection stops the advertising set it came through; `StartAdvertisingAsync(..., autoRestart: true)` starts it again once that central disconnected.

```csharp
var service = new LocalGattServiceBuilder(0x180D);
LocalGattCharacteristic measurement = service.AddCharacteristic(0x2A37, GattProperty.Notify);
service.AddCharacteristic(0x2A39, GattProperty.Write, onWrite: (central, value, ct) => ValueTask.FromResult(GattProtocolStatus.Success));
await adapter.Peripheral.Gatt.AddServiceAsync(service);

await using AdvertisingSet advertising = await adapter.Broadcaster.StartAdvertisingAsync(
    AdvertisingData.Empty.WithCompleteLocalName("Sensor"),
    interval: TimeSpan.FromMilliseconds(100),
    autoRestart: true);
```

`adapter.Peripheral.DeviceName` and `Appearance` set what centrals read from the GAP service, which the backend serves itself, on adapters with `Capabilities.SetGapIdentity`.
HciHost and the mock also serve the GATT service: Service Changed tells centrals that turned it on when a service was added, and Database Hash changes with the services. `IncludeGattService = false` in their options leaves it out, e.g. to simulate a device without it.
The library brings contracts for the battery, heart rate, device information and echo services, e.g. `AddBatteryServiceAsync()` on the peripheral side and `DiscoverBatteryServiceAsync()` on the central side; `RemoteEchoService.QueryOneAsync(request)` writes a request and returns the reply.

### Diagnostics

The library logs through the `ILoggerFactory` you pass to `BleManager`, traces on the `Darp.Ble` activity source and measures on the `Darp.Ble` meter. [docs/telemetry.md](docs/telemetry.md) lists every span, tag, metric and event id; those names only change additively.

```csharp
tracing.AddSource(BleTelemetry.SourceName);
metrics.AddMeter(BleTelemetry.MeterName);
var manager = new BleManager(builder => builder.AddMock().ConfigureDiagnostics(options => options.IncludeAddresses = false));
```

With SerilogTracing, `Instrument.With<DarpBleActivityInstrumentor>()` from `Darp.Ble.SerilogTracing` renders each span with its template, e.g. `Read "2A19"`.

## Backends

The roles behave the same on every backend; where a platform decides something itself, the backend says so in its docs.

- **HciHost** drives an HCI controller, by default the nRF52840 dongle over a serial port, and supports all four roles.
- **WinRT** needs Windows 10 2004 or later, and an app that targets `net10.0-windows10.0.19041.0`; on other platforms it offers no adapter.
  Windows chooses the advertising addresses and intervals, sends no scan response data of the app's own, and keeps advertising when a central connects.
  It serves a hosted service only while it advertises it, so published services are live while a connectable advertising set runs, and such a set takes no data of the app's own.
  A request counts as sent once its Windows call started, and disposing a connection ends it for this app while Windows may keep the link for others.
- **Android** scans, on Android 12 or later; the other roles are not ported to 2.0 yet. Android chooses the scan type and timing itself, and the backend reports legacy advertisements only.
  The app needs the `BLUETOOTH_SCAN` permission granted, and either location access with location services on, or `android:usesPermissionFlags="neverForLocation"` on that permission in its manifest; with `neverForLocation`, Android leaves out some beacon advertisements. A scan that needs location services while they are off fails.
- **Mock** runs in-process for tests, with simulated peripherals added through `AddMock(...)`.

## Migrating from 1.x

[docs/migration.md](docs/migration.md) shows the changes from 1.x with code before and after: registering backends, async streams instead of observables, notifications, typed errors, hosting and tests.

## Further Documentation

Additional documentation exists in form of the unit tests and the [examples](examples).

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

# Install the android workload
dotnet workload install android
```

## About

Darp.Ble is a project by [rosslight GmbH](https://engineering.rosslight.de/), see also [darp.ble Product Page](https://engineering.rosslight.de/ble-suite)
