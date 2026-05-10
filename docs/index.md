# Darp.Ble

Darp.Ble is a .NET abstraction for Bluetooth Low Energy. It gives you one common entry point for working with BLE devices while keeping the platform-specific behavior in implementation packages.

The API is designed around explicit device initialization and reactive flows for observing advertisements, connecting to peripherals, and receiving updates.

## Install the packages

Install the core package and one implementation package for the runtime you want to use.

```powershell
dotnet add package Darp.Ble
dotnet add package Darp.Ble.WinRT
```

| Package | Use when                                                                        |
| --- |---------------------------------------------------------------------------------|
| `Darp.Ble.WinRT` | You want to use the built-in Windows BLE stack.                                 |
| `Darp.Ble.Android` | You are targeting Android.                                                      |
| `Darp.Ble.HciHost` | You want to use a BLE adapter through an HCI host, for example a serial dongle. |
| `Darp.Ble.Mock` | You want a local mock implementation for tests or development.                  |

See [Platforms](platforms/) for platform-specific capabilities and setup notes.

## Create a manager

Create a `BleManager` and register the implementation factories you want to use.

```csharp
using Darp.Ble;
using Darp.Ble.WinRT;
using Darp.Ble.HciHost;
using Darp.Ble.Mock;

var bleManager = new BleManagerBuilder()
    .AddWinRT()
    .AddSerialHciHost()
    .AddMock()
    .CreateManager();
```

`BleManager` uses the registered factories to enumerate available BLE devices. You can register more than one implementation if your application should support multiple backends.

## Initialize a device

Enumerating devices does not open the device yet. Pick a device, initialize it, and dispose it when you are done.

```csharp
await using IBleDevice bleDevice = bleManager.EnumerateDevices().First();

await bleDevice.InitializeAsync();

// Use bleDevice.Observer, bleDevice.Central, bleDevice.Broadcaster, or bleDevice.Peripheral.
```

The available roles depend on the selected implementation and hardware. For example, a device may support observing advertisements, acting as a central, advertising, or hosting local GATT services.

## What to do next

- Choose a backend in [Platforms](platforms/).
- Scan for advertisements in [Scanning](guides/scanning/).
- Connect to peripherals in [Connecting](guides/connecting/).
- Receive characteristic updates in [Notifications](guides/notifications/).
- Advertise local data in [Advertising](guides/advertising/).
- Build tests with [Testing with mock](guides/testing-with-mock/).
