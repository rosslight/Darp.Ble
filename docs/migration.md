# Migrating from Darp.Ble 1.x to 2.0

2.0 keeps the concepts of 1.x (adapters, the four roles, characteristic declarations) and changes how an app holds them: adapters open ready to use, scans and notifications are async streams instead of observables, and errors carry a typed reason. This guide walks through the changes in the order an app meets them.

## Register backends and open an adapter

The manager takes its backends in its constructor. Opening an adapter returns it ready to use, or throws a `BleOpenException` whose `Kind` says why. There is no separate initialization step and no result to check.

```csharp
// 1.x
var bleManager = new BleManagerBuilder().AddWinRT().AddSerialHciHost().CreateManager();
IBleDevice device = bleManager.EnumerateDevices().First();
InitializeResult result = await device.InitializeAsync();

// 2.0
var bleManager = new BleManager(builder => builder.AddWinRT().AddSerialHciHost());
IReadOnlyList<BleAdapterInfo> adapters = await bleManager.EnumerateAdaptersAsync(ct);
await using BleAdapter adapter = await adapters[0].OpenAsync(ct);   // BleOpenException: NotAvailable, Disabled, MissingPermission
```

- `IBleDevice` is now `BleAdapter`, a sealed class. You own it: disposing it closes it and ends its scans, connections and advertising.
- `BleAdapterInfo.Backend` names the backend (`BleBackends.HciHost`, `BleBackends.WinRT`, ...), so an app chooses an adapter without opening it.
- With dependency injection, `services.AddBleManager(builder => ...)` registers the manager with the container's `ILoggerFactory` and `TimeProvider`.
- The factories are gone: `HciHostBleFactory`, `SerialHciHostBleFactory` and `SingleSerialHciHostBleFactory` become `AddSerialHciHost(...)` and `AddHciHost(transport)`; `WinBleFactory` becomes `AddWinRT()`; `AndroidBleFactory` becomes `AddAndroid()`.

## Scan

A scan runs while you enumerate it. Ending the loop or cancelling the token ends it, so there is no subscription and no separate start and stop to keep in step.

```csharp
// 1.x
IDisposable subscription = device.Observer.OnAdvertisement().Subscribe(advertisement => Show(advertisement));
await device.Observer.StartObservingAsync(ct);
// ...
await device.Observer.StopObservingAsync();
subscription.Dispose();

// 2.0
await foreach (IGapAdvertisement advertisement in adapter.Observer.ScanAsync(ct))
    Show(advertisement);
```

- Every scan has its own queue, and all scans of an adapter share one radio scan. A slow loop delays only itself; when its queue is full, the oldest advertisements are dropped unless you pass other `QueueOptions`.
- The radio settings go to `adapter.Observer.ConfigureAsync(parameters)` and apply to every scan.
- Connecting pauses the adapter's scans; they continue once the connection is up.

## Connect

`ConnectAsync` returns the connection once the link is up and the MTU is agreed. The connection is yours to dispose; `Disconnected` completes once with the reason, also when the peripheral disconnects first.

```csharp
// 1.x
IGattServerPeer peer = await device.Central.ConnectToPeripheral(address).FirstAsync().ToTask(ct);

// 2.0
await using ConnectedPeripheral connection = await adapter.Central.ConnectAsync(address, cancellationToken: ct);
RemoteGattService service = await connection.Gatt.DiscoverServiceAsync(0x180F, ct);
```

A failed attempt throws a `BleConnectException` whose `Kind` is `Unreachable`, `TimedOut`, `Rejected` or `Busy`, so a retry can tell an attempt that may succeed later from one that never will.

## Read and write

Reads and writes look as before. Requests on one connection run one at a time, in call order, so overlapping calls are safe. A request whose peer does not answer within 30 seconds ends the connection with `DisconnectReason.AttTimeout`.

A write without response is now awaited. It completes once the backend accepted the packet, so a loop of writes is paced by what the radio can send instead of filling a buffer.

```csharp
// 1.x
characteristic.WriteWithoutResponse(chunk);

// 2.0
await characteristic.WriteWithoutResponseAsync(chunk, ct);
```

## Notifications

Listening and enabling are two steps now. `Subscribe()` creates a stream without any I/O, so it exists before the first notification arrives. `EnableNotificationsAsync()` writes the CCCD, once per connection. A stream ends with its connection.

```csharp
// 1.x: enables on the first handle, disables when the last one is disposed
await using IDisposableObservable<byte[]> notifications = await characteristic.OnNotifyAsync(ct);
using IDisposable subscription = notifications.Subscribe(value => Show(value));

// 2.0
using NotificationStream<byte[]> stream = characteristic.Subscribe();
await characteristic.EnableNotificationsAsync(ct);
await foreach (byte[] value in stream.WithCancellation(ct))
    Show(value);
```

Each stream has its own queue, so a slow consumer no longer stalls the backend's read loop.

## Cancellation and errors

- `OperationCanceledException` only ever comes from your own token. A call cancelled while it waits in the queue sends nothing; a request that is already sent runs to its answer, and the connection stays usable.
- Requests of an ended connection throw a `BleDisconnectedException` with the connection's `DisconnectInfo`.
- A peer's error answer throws a `GattException` with its `AttStatus`.

Code that searched exception messages, or retried on any exception, can match these types instead.

## Host a service

A service is described completely with a `LocalGattServiceBuilder` and published in one call. A builder that has published its service cannot be changed any more.

```csharp
// 1.x
IGattClientService service = device.Peripheral.AddService(0x180D);
service.AddCharacteristic<Properties.Write>(0x2A39, onWrite: async (peer, value) => GattProtocolStatus.Success);

// 2.0
var service = new LocalGattServiceBuilder(0x180D);
LocalGattCharacteristic measurement = service.AddCharacteristic(0x2A37, GattProperty.Notify);
service.AddCharacteristic(0x2A39, GattProperty.Write,
    onWrite: (central, value, ct) => ValueTask.FromResult(GattProtocolStatus.Success));
await adapter.Peripheral.Gatt.AddServiceAsync(service, ct);
await measurement.NotifyAllAsync(new byte[] { 0x00, 72 }, ct);   // every central that turned notifications on
```

- Handlers get the `ConnectedCentral` that asked and a token that is cancelled when its connection ends. The handlers of one central run one at a time.
- `central.GetOrAddState(() => new Session())` keeps state for one connection and disposes it once the connection ended and its handlers returned. This replaces fields shared by every central and reset on any disconnect.
- `adapter.Peripheral.DeviceName` and `Appearance` set what centrals read from the GAP service, on adapters with `Capabilities.SetGapIdentity`. Windows serves its own GAP service, so WinRT throws `NotSupportedException` there.
- `peripheral.AddGattService()` is gone: HciHost and the mock serve the GATT service with Service Changed and Database Hash themselves. `IncludeGattService = false` in `HciHostOptions`, `SerialHciHostOptions`, `MockBackendOptions` or `AddPeripheral(..., includeGattService: false)` leaves it out.
- `AddUserDescription(text)` adds a user description that centrals can read but not write.

## Test with the mock backend

Simulated peripherals are adapters of their own, configured when the mock backend is registered. `MockBle.OpenAdapterAsync()` opens a mock adapter in one line.

```csharp
// 1.x
builder.AddMock(factory => factory.AddPeripheral(async device => device.Peripheral.AddHeartRateService()));

// 2.0
var manager = new BleManager(builder => builder.AddMock(mock => mock.AddPeripheral(async peripheral =>
{
    await peripheral.Peripheral.Gatt.AddHeartRateServiceAsync();
    await peripheral.Broadcaster.StartAdvertisingAsync(data, interval: TimeSpan.FromMilliseconds(100), autoRestart: true);
})));
```

Scans, connections and failures on the mock follow the same rules as on the other backends, so a test can await the first advertisement of `ScanAsync` instead of waiting for a callback.

## Backend authors

Backends no longer derive from `BleDevice`, `BleObserver` and the other base classes. They implement the drivers in `Darp.Ble.Backends` (`IBleBackend`, `IBleAdapterDriver`, `IObserverDriver`, ...) and register with `BleManagerBuilder.AddBackend`. The core owns the queues, timeouts, lifetimes and diagnostics; the interfaces' docs state what a driver must guarantee.

## Backend support

| Backend | Observer | Broadcaster | Central | Peripheral | Notes |
| --- | --- | --- | --- | --- | --- |
| `Darp.Ble.HciHost` | ✓ | ✓ | ✓ | ✓ | nRF52840 dongle or any HCI controller over a transport of your own |
| `Darp.Ble.WinRT` | ✓ | ✓ | ✓ | ✓ | Windows 10 2004 or later; see the README for what Windows decides itself |
| `Darp.Ble.Android` | ✓ | | | | Android 12 or later; scanning only in 2.0 |
| `Darp.Ble.Mock` | ✓ | ✓ | ✓ | ✓ | In-process, for tests |
