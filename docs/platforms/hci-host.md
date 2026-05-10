# HciHost

`Darp.Ble.HciHost` talks to a BLE controller through an HCI transport. The common path is H4 over a serial port.

## Install

```powershell
dotnet add package Darp.Ble
dotnet add package Darp.Ble.HciHost
```

The package targets `net9.0` and uses `System.IO.Ports` for serial transports.

## Requirements

- The controller must speak HCI.
- The process must be allowed to open the serial port or custom transport.
- Use `AddHciHost(transportLayer)` when serial H4 is not the right transport.

!!! info "Serial auto-discovery"

    `AddSerialHciHost()` auto-enumerates only USB ports whose vendor/product ID appears in `DeviceNameMapping`. The default mapping contains `(0x2FE3, 0x0004) -> "nrf52840 dongle"`. Use `AddSerialHciHost(portName)` when you already know the port.

## Registration

Auto-enumerate mapped USB serial devices:

```csharp
using Darp.Ble;
using Darp.Ble.HciHost;

var manager = new BleManagerBuilder()
    .AddSerialHciHost()
    .CreateManager();
```

Open one known port:

```csharp
var manager = new BleManagerBuilder()
    .AddSerialHciHost("COM11", factory => factory.DeviceName = "Local HCI host")
    .CreateManager();
```

Use a custom HCI transport:

```csharp
var manager = new BleManagerBuilder()
    .AddHciHost(transportLayer)
    .CreateManager();
```

## Capabilities

After initialization, HciHost creates `Observer`, `Central`, `Broadcaster`, and `Peripheral`.

The backend also sets a random address on the controller. Configure `RandomAddress`, `Settings`, or device naming on the factory when your hardware or tests need stable values.

Advertising support uses the controller-reported maximum advertising data length.

## Next steps

- Use [Scanning](../../guides/scanning/) for observer workflows.
- Use [Connecting](../../guides/connecting/) for central workflows.
- Use [Advertising](../../guides/advertising/) for broadcaster/peripheral workflows.
