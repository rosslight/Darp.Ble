# Examples

Each example is a single C# file that shows one task. Run one from the repository root with the .NET 10 SDK:

```sh
dotnet run examples/list-adapters.cs
```

The first run builds the library and may print its analyzer warnings; the example's own output follows them.

| Example | What it shows |
| --- | --- |
| [`list-adapters.cs`](list-adapters.cs) | List the adapters of the registered backends |
| [`open-adapter.cs`](open-adapter.cs) | Open an adapter by name, print its capabilities, and handle a failed open |
| [`scan.cs`](scan.cs) | Scan for a few seconds and print every advertisement |
| [`advertise.cs`](advertise.cs) | Advertise a name until Ctrl+C |
| [`connect.cs`](connect.cs) | Find a peripheral by its service, connect, list its services and disconnect |
| [`connect-retry.cs`](connect-retry.cs) | Retry a connect while the failure is transient and reconnect when the link is lost |
| [`host-service.cs`](host-service.cs) | Host a service with a write handler, per-central state and notifications to every subscribed central, until Ctrl+C |
| [`read-write.cs`](read-write.cs) | Read a typed value, write a new one and read it back |
| [`notifications.cs`](notifications.cs) | Subscribe to a heart rate sensor through its typed client and print the decoded measurements |
| [`request-response.cs`](request-response.cs) | Host an echo service and query it: write a request, get the reply as a notification |

## On hardware

The examples use the in-process mock backend, so they run without Bluetooth hardware.
To run one on a real adapter, swap the backend in three places:

| Mock | HCI dongle on a serial port | Windows |
| --- | --- | --- |
| `#:project ../src/Darp.Ble.Mock/Darp.Ble.Mock.csproj` | `#:project ../src/Darp.Ble.HciHost/Darp.Ble.HciHost.csproj` | `#:project ../src/Darp.Ble.WinRT/Darp.Ble.WinRT.csproj` and `#:property TargetFramework=net10.0-windows10.0.19041.0` |
| `using Darp.Ble.Mock;` | `using Darp.Ble.HciHost;` | `using Darp.Ble.WinRT;` |
| `builder.AddMock(...)` | `builder.AddSerialHciHost()` | `builder.AddWinRT()` |

Outside this repository, reference the package instead of the project, e.g. `#:package Darp.Ble.HciHost@<version>`.
`AddSerialHciHost()` finds the dongles listed in `SerialHciHostOptions.DeviceNameMapping` (an nRF52840 dongle with HCI
firmware by default); `AddSerialHciHost("COM3")` takes the adapter on that port.
The simulated peripheral that `AddMock(...)` sets up goes away with it, so an example that connects needs a real device
that does what the simulated one did. Without an adapter, the examples that open one print `No adapter found` and exit with code 1.
