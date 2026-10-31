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

## Install

Add the package of each backend you use; each brings the core package `Darp.Ble`.

```sh
dotnet add package Darp.Ble.HciHost   # an HCI controller on a serial port, e.g. an nRF52840 dongle (Windows, Linux)
dotnet add package Darp.Ble.WinRT     # Windows' Bluetooth adapter
dotnet add package Darp.Ble.Android   # Android's Bluetooth adapter
dotnet add package Darp.Ble.Mock      # an in-process backend with simulated peripherals, for tests and trying things out
```

A first scan on hardware, which handles finding no adapter:

```csharp
using Darp.Ble;
using Darp.Ble.Gap;
using Darp.Ble.HciHost;

var manager = new BleManager(builder => builder.AddSerialHciHost());
IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync();
BleAdapterInfo? info = adapters.FirstOrDefault();
if (info is null)
{
    Console.Error.WriteLine("No adapter found");
    return 1;
}

await using BleAdapter adapter = await info.OpenAsync();
using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    await foreach (IGapAdvertisement advertisement in adapter.Observer.ScanAsync(stop.Token))
        Console.WriteLine($"{advertisement.Address} {advertisement.Rssi} dBm");
}
catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
return 0;
```

With WinRT it is `builder.AddWinRT()` (namespace `Darp.Ble.WinRT`), and the app targets `net10.0-windows10.0.19041.0`; a file-based app says so with `#:property TargetFramework=net10.0-windows10.0.19041.0`.

## Features

### Open an adapter

A `BleManager` lists the adapters of the backends you register. Opening one returns a ready `BleAdapter`, or throws a `BleOpenException` whose `Kind` says why (`NotAvailable`, `Disabled`, `MissingPermission`).
You own the adapter: disposing it closes it.

```csharp
var bleManager = new BleManager(builder => builder.AddMock());

IReadOnlyList<BleAdapterInfo> adapters = await bleManager.EnumerateAdaptersAsync();
await using BleAdapter adapter = await adapters[0].OpenAsync();
```

`builder.AddSerialHciHost()` adds an adapter for every HCI controller on a USB serial port whose USB ids are in `DeviceNameMapping`; by default those of an nRF52840 dongle running Zephyr's `hci_uart` sample. A dongle with other USB ids, e.g. with the firmware it is shipped with, is not found; a port that matches still needs HCI firmware to open. `builder.AddSerialHciHost("COM3")` adds the one on that port.
[docs/hcihost.md](https://github.com/rosslight/Darp.Ble/blob/main/docs/hcihost.md) says which firmware to flash, how to add other controllers and what Linux needs, e.g. access to the port through the `dialout` group.
`builder.AddWinRT()` adds Windows' Bluetooth adapter and `builder.AddAndroid()` Android's; see [Backends](#backends) for what each one supports.
With dependency injection, `services.AddBleManager(builder => builder.AddMock())` registers the same manager and uses the container's `ILoggerFactory` and `TimeProvider`.
Open each adapter once and share the open `BleAdapter`: the mock throws an `InvalidOperationException` when it is opened again while it is open, and Windows opens a serial port for one process only.
In an app with dependency injection, one service owns the adapter and the others get it from there:

```csharp
services.AddSingleton<AdapterOwner>();
services.AddHostedService(provider => provider.GetRequiredService<AdapterOwner>());

// Opens the adapter when the app starts and closes it when the container is disposed
sealed class AdapterOwner(BleManager manager) : IHostedService, IAsyncDisposable
{
    private readonly BleManager _manager = manager;
    private readonly TaskCompletionSource<BleAdapter> _adapter = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<BleAdapter> Adapter => _adapter.Task;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<BleAdapterInfo> adapters = await _manager.EnumerateAdaptersAsync(cancellationToken);
        BleAdapterInfo info = adapters.FirstOrDefault() ?? throw new InvalidOperationException("No adapter found");
        _adapter.SetResult(await info.OpenAsync(cancellationToken));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (_adapter.Task.IsCompletedSuccessfully)
            await _adapter.Task.Result.DisposeAsync();
    }
}
```

The [examples](https://github.com/rosslight/Darp.Ble/tree/main/examples) are single-file programs for one task each, e.g. `dotnet run examples/list-adapters.cs`.

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

### Connect

`adapter.Central.ConnectAsync(address)` returns a `ConnectedPeripheral` once the link is up and the MTU is agreed, or throws a `BleConnectException` whose `Kind` says why (`Unreachable`, `TimedOut`, `Rejected`, `Busy`).
It has no time limit of its own: HciHost and WinRT keep trying until the token is cancelled, so pass one that times out.
You own the connection: disposing it disconnects. `Disconnected` completes once with the reason, also when the peripheral disconnects first.
The adapter's scans pause while it connects and continue afterwards.

```csharp
using var connecting = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await using ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
    advertisement.Address,
    cancellationToken: connecting.Token);

RemoteGattService service = await connection.DiscoverServiceAsync(0x180D);
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
A stream ends with its connection: normally when you disconnected, with a `BleDisconnectedException` when the link was lost.

```csharp
using NotificationStream<byte[]> stream = measurement.Subscribe();
await measurement.EnableNotificationsAsync();
await foreach (byte[] value in stream)
    Console.WriteLine(Convert.ToHexString(value));
```

Each stream has its own queue of 1,000 values. A stream that falls further behind, also one that is subscribed but not read yet, ends with a `BleQueueOverflowException` after the queued values.
For a high-rate characteristic, pass a queue that fits, e.g. one that drops the oldest values: `measurement.Subscribe(new QueueOptions(10_000, QueueFullMode.DropOldest))`; `builder.ConfigureQueues(queues => queues.Notifications = ...)` changes the default.

### Host a service

`adapter.Peripheral.AddServiceAsync(builder)` adds a complete service. `ConnectedCentrals` lists the centrals that are connected; `WatchCentralsAsync()` yields them, then each central that connects. A central's `Disconnected` completes once it left.
Read and write handlers get the `ConnectedCentral` that asked and a token that is cancelled when its connection ends.
Declarations work on this side too: a typed declaration decodes what centrals write and encodes what handlers return. `NotifyAsync(central, value)` notifies one central, `NotifyAllAsync(value)` every central that turned notifications on.
`central.GetOrAddState(() => new Session())` keeps state for one connection and disposes it once the connection ended and its handlers returned.
A connection stops the advertising set it came through; `StartAdvertisingAsync(..., autoRestart: true)` starts it again once that central disconnected.

```csharp
var service = new LocalGattServiceBuilder(0x180D);
LocalGattCharacteristic measurement = service.AddCharacteristic(0x2A37, GattProperty.Notify);
service.AddCharacteristic(0x2A39, GattProperty.Write, onWrite: (central, value, ct) => ValueTask.FromResult(GattProtocolStatus.Success));
await adapter.Peripheral.AddServiceAsync(service);

await using AdvertisingSet advertising = await adapter.Broadcaster.StartAdvertisingAsync(
    AdvertisingData.Empty.WithCompleteLocalName("Sensor"),
    interval: TimeSpan.FromMilliseconds(100),
    autoRestart: true);
```

`adapter.Peripheral.DeviceName` and `Appearance` set what centrals read from the GAP service, which the backend serves itself, on adapters with `Capabilities.SetGapIdentity`.
HciHost and the mock also serve the GATT service: Service Changed tells centrals that turned it on when a service was added, and Database Hash changes with the services. `IncludeGattService = false` in their options leaves it out, e.g. to simulate a device without it.
The library brings contracts for the battery, heart rate, device information and echo services, e.g. `AddBatteryServiceAsync()` on the peripheral side and `DiscoverBatteryServiceAsync()` on the central side; `RemoteEchoService.QueryOneAsync(request)` writes a request and returns the reply.

### Threads

Hosted read and write handlers run on thread-pool threads: one at a time for each central, at the same time for different centrals.
State that several centrals share needs a lock; state of one central belongs in `central.GetOrAddState(...)`.
The library never runs app code on a backend's read loop, so a handler may block without stalling the backend.
Awaited calls and `await foreach` loops over scans and notification streams resume like any `await`: on the caller's context, e.g. a UI thread, unless the caller used `ConfigureAwait(false)`.

### Test with the mock

`await MockBle.OpenAdapterAsync()` opens a mock adapter in one line. `AddMock(mock => mock.AddPeripheral(...))` adds simulated peripherals; each is a full adapter of its own, which advertises into the mock adapter's scans, hosts services, and can also connect as a central to the services the app hosts.
A peripheral is set up when the mock adapter opens, so a test gets hold of it through a `TaskCompletionSource` the test owns.
A `MockRange` takes the peripheral out of range: `Leave()` ends its links with `DisconnectReason.ConnectionTimeout` on both sides and makes it unreachable until `Enter()`, so a test can check how the app handles a lost link and connects again.

```csharp
var range = new MockRange();
var sensor = new TaskCompletionSource<AdvertisingSet>();
var manager = new BleManager(builder => builder.AddMock(mock => mock.AddPeripheral(
    async (simulated, _) => sensor.SetResult(await simulated.Broadcaster.StartAdvertisingAsync(
        AdvertisingData.Empty.WithCompleteLocalName("Sensor"),
        interval: TimeSpan.FromMilliseconds(100),
        autoRestart: true)),
    "Sensor",
    range: range)));
IReadOnlyList<BleAdapterInfo> adapters = await manager.EnumerateAdaptersAsync(ct);
await using BleAdapter adapter = await adapters[0].OpenAsync(ct);
AdvertisingSet set = await sensor.Task;

await using ConnectedPeripheral connection = await adapter.Central.ConnectAsync(set.RandomAddress, cancellationToken: ct);
range.Leave();
DisconnectInfo lost = await connection.Disconnected;   // ConnectionTimeout; connecting now fails as Unreachable
range.Enter();
// Connect once it is heard again: autoRestart starts its advertising after the lost link has ended on its side too
await adapter.Observer.ScanAsync(ct).Where(advertisement => advertisement.Address == set.RandomAddress).FirstAsync(ct);
await using ConnectedPeripheral again = await adapter.Central.ConnectAsync(set.RandomAddress, cancellationToken: ct);
```

The mock fails a connection to an unreachable peripheral at once; on hardware, `ConnectAsync` keeps trying until its token is cancelled (see [Connect](#connect)).

### Diagnostics

The library logs through the `ILoggerFactory` you pass to `BleManager`, traces on the `Darp.Ble` activity source and measures on the `Darp.Ble` meter. [docs/telemetry.md](https://github.com/rosslight/Darp.Ble/blob/main/docs/telemetry.md) lists every span, tag, metric and event id; those names only change additively.

```csharp
tracing.AddSource(BleTelemetry.SourceName);
metrics.AddMeter(BleTelemetry.MeterName);
var manager = new BleManager(builder => builder.AddMock().ConfigureDiagnostics(options => options.IncludeAddresses = false));
```

With SerilogTracing, `Instrument.With<DarpBleActivityInstrumentor>()` from `Darp.Ble.SerilogTracing` renders each span with its template, e.g. `Read "2A19"`.

## Backends

The roles behave the same on every backend; where a platform decides something itself, the backend says so in its docs.

- **HciHost** drives an HCI controller over a serial port, e.g. an nRF52840 dongle with Zephyr's `hci_uart` sample ([setup](https://github.com/rosslight/Darp.Ble/blob/main/docs/hcihost.md)), and supports all four roles.
- **WinRT** needs Windows 10 2004 or later, and an app that targets `net10.0-windows10.0.19041.0`; on other platforms it offers no adapter.
  Windows chooses the advertising addresses and intervals, sends no scan response data of the app's own, and keeps advertising when a central connects.
  It serves a hosted service only while it advertises it, so added services are live while a connectable advertising set runs, and such a set takes no data of the app's own.
  A request counts as sent once its Windows call started, and disposing a connection ends it for this app while Windows may keep the link for others.
  When the Bluetooth radio turns off, scans end with a transient `BleException` and non-connectable advertising sets stop until they are started again; connectable sets advertise again once the radio is back. Starting a scan or a connectable set while the radio is off throws a transient `BleException`.
- **Android** scans, on Android 12 or later; the other roles are not ported to 2.0 yet. Android chooses the scan type and timing itself, and the backend reports legacy advertisements only.
  The app needs the `BLUETOOTH_SCAN` permission granted, and either location access with location services on, or `android:usesPermissionFlags="neverForLocation"` on that permission in its manifest; with `neverForLocation`, Android leaves out some beacon advertisements. A scan that needs location services while they are off fails.
- **Mock** runs in-process for tests, with simulated peripherals added through `AddMock(...)`.

The driver layer that backends implement, `Darp.Ble.Backends`, is experimental: using it reports `DARPBLE0001`, and it may change in a minor release.

## Further Documentation

Additional documentation exists in form of the unit tests and the [examples](https://github.com/rosslight/Darp.Ble/tree/main/examples).

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
