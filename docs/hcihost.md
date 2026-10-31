# HciHost setup

`Darp.Ble.HciHost` runs the Bluetooth host in your process and talks HCI to a controller over a serial port (H4).
It works with an nRF52840 dongle running Zephyr's [`hci_uart` sample](https://docs.zephyrproject.org/latest/samples/bluetooth/hci_uart/README.html), which on the dongle is a USB serial port with the USB ids `2FE3:0004`:

```sh
west build -b nrf52840dongle/nrf52840 samples/bluetooth/hci_uart
```

Flash it as Zephyr's documentation for the dongle describes. A dongle with the firmware it is shipped with has other USB ids, so `AddSerialHciHost()` does not find it. The ids are all it checks: other firmware that keeps Zephyr's USB ids is listed, and opening it fails because it does not speak HCI.

## Find the controller

`builder.AddSerialHciHost()` lists the USB serial ports and adds an adapter for every port whose vendor and product id are in `SerialHciHostOptions.DeviceNameMapping`.
By default that holds `2FE3:0004`, so the dongle shows up as `nrf52840 dongle (/dev/ttyACM0)`. Other controllers are added to the mapping:

```csharp
var manager = new BleManager(builder =>
    builder.AddSerialHciHost(options => options.DeviceNameMapping[(0x1234, 0x5678)] = "My controller"));
```

`builder.AddSerialHciHost("/dev/ttyACM0")` (or `"COM3"`) adds the controller on that port without looking at its USB ids.
Listing ports works on Windows and Linux; elsewhere `EnumerateAdaptersAsync` throws a `NotSupportedException`, so pass the port.

The port opens with the defaults of .NET's `SerialPort` (9600 baud, no flow control) and the library sets nothing else. A USB serial port like the dongle's ignores these settings.
A controller on a physical UART, e.g. `hci_uart` on a development kit at 1 Mbit/s with RTS/CTS, needs a transport of your own: `builder.AddHciHost(transport)` takes any `ITransportLayer`.

## Linux

- Listing ports reads each port's USB ids with `udevadm info`, so `udevadm` must be on the `PATH`. A port it cannot read is skipped.
- The user needs read and write access to the port, usually through the group that owns it (`ls -l /dev/ttyACM0`), e.g. `sudo usermod -aG dialout $USER` and logging in again.
  Without it, opening throws a `BleOpenException` with `Kind` `MissingPermission`.

## Windows

The dongle is a COM port. Windows opens a serial port for one process only, so opening fails with `NotAvailable` while another program, e.g. a serial terminal, holds it.
