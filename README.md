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
| Darp.Ble.HciHost | X        | X       | x           | x          | [![Darp.Ble.HciHost](https://img.shields.io/nuget/v/Darp.Ble.HciHost.svg)](https://www.nuget.org/packages/Darp.Ble.HciHost/) |
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
    .AddWinRT()
    .AddSerialHciHost())
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
var connectableObservable = observer.Publish();
connectableObservable.Subscribe(advertisement =>
{
    Console.WriteLine($"Received advertisement from {advertisement.Address}");
});
var disposable = connectableObservable.Connect();
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
dotnet build -t:InstallAndroidDependencies -f net8.0-android -p:AndroidSdkDirectory=c:\work\android-sdk -p:JavaSdkDirectory=c:\work\jdk -p:AcceptAndroidSdkLicenses=True
setx JAVA_HOME C:\work\jdk\
setx ANDROID_HOME C:\work\android-sdk\

# Restore android workload
dotnet workload restore
```