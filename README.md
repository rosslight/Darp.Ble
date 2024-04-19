# Darp.Ble

[![.NET](https://github.com/rosslight/Darp.Ble/actions/workflows/test_and_publish.yml/badge.svg)](https://github.com/rosslight/Darp.Ble/actions/workflows/test_and_publish.yml)
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
| Darp.Ble.HciHost | X        |         |             |            | [![Darp.Ble.HciHost](https://img.shields.io/nuget/v/Darp.Ble.HciHost.svg)](https://www.nuget.org/packages/Darp.Ble.HciHost/) |
| Darp.Ble.iOS     |          |         |             |            | planned                                                                                                                      |
| Darp.Ble.Mac     |          |         |             |            | planned                                                                                                                      |
| Darp.Ble.BlueZ   |          |         |             |            | planned                                                                                                                      |
| Darp.Ble.Mock    | X        | X       |             |            | [![Darp.Ble.Mock](https://img.shields.io/nuget/v/Darp.Ble.Mock.svg)](https://www.nuget.org/packages/Darp.Ble.Mock/)          |

## Features

### Initialize BLE device

To get a BleDevice, you will need a BleManager. When creating it implementation specific factories are registered.
`EnumerateDevices` then looks through all defined implementations, yields them to the user and allows for device selection.
Enumerating the devices does not connect to it yet.

This is done when calling `InitializeAsync`. After that, the device will be usable.
To release resources call `DisposeAsync`.

```csharp
var bleManager = new BleManagerBuilder()
    .With<WinBleFactory>()
    .With(new HciHostBleFactory("COM7"))
    .CreateManager();

var bleDevice = bleManager.EnumerateDevices().First();
await bleDevice.InitializeAsync();

...

await bleDevice.DisposeAsync();
```

### Observer

If the device supports observer mode, use the `Observer` property. The observer works as a connectable observable.
Subscribe to it to receive events and connect to actually start the scan.
To stop the scan, dispose of the return of the connection or use `StopScan`.

```csharp
var observer = bleDevice.Observer;
observer.Configure(new BleScanParameters
{
    ScanType = ScanType.Active,
    ScanInterval = ScanTiming.Ms100,
    ScanWindow = ScanTiming.Ms100,
});
observer.Subscribe(advertisement =>
{
    Console.WriteLine($"Received advertisement from {advertisement.Address}");
});
var disposable = observer.Connect();
```

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

var service = await peerDevice.DiscoverServiceAsync(new BleUuid(0x1234));
var writeChar = await service.DiscoverCharacteristicAsync<Properties.Write>(new BleUuid(0x5678));
var notifyChar = await service.DiscoverCharacteristicAsync<Properties.Notify>(new BleUuid(0xABCD));
var notifyTask = notifyChar.OnNotify().RefCount().FirstAsync().ToTask();
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

## Create new release
To create a release, simply push a new tag with pattern `vX.Y.Z`. This will trigger a workflow releasing the new version.
```shell
git tag vX.Y.Z
git push origin vX.Y.Z
```

