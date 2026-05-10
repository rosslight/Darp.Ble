# Platforms

Darp.Ble has one shared API and multiple backends. Pick the backend by the thing that owns BLE in your app: Windows, Android, an HCI controller, or the in-process mock.

## Backend matrix

| Backend | Package | Registration | What it actually gives you |
| --- | --- | --- | --- |
| [WinRT](winrt/) | `Darp.Ble.WinRT` | `AddWinRT()` | The default Windows BLE adapter. Roles depend on Windows adapter capability flags. |
| [Android](android/) | `Darp.Ble.Android` | `Add(new AndroidBleFactory(...))` | Android scanner/observer support when BLE, Bluetooth, and scan permission are available. |
| [HciHost](hci-host/) | `Darp.Ble.HciHost` | `AddSerialHciHost()` or `AddHciHost(...)` | A BLE host backed by an HCI controller. Provides observer, central, broadcaster, and peripheral roles after initialization. |
| [Mock](mock/) | `Darp.Ble.Mock` | `AddMock()` | A test backend: the primary mock scans/connects, configured mock peripherals advertise/serve local GATT data. |

## Check capabilities after initialization

Capabilities are not final until `InitializeAsync()` has run.

```csharp
await bleDevice.InitializeAsync();

if (bleDevice.Capabilities.HasFlag(Capabilities.Observer))
{
    // Safe to use bleDevice.Observer
}
```

Accessing an unsupported role throws. Check first, especially with WinRT and Android.

## Next steps

- Read the backend page before writing setup code.
- Use [Scanning](../guides/scanning/) for observer workflows.
- Use [Connecting](../guides/connecting/) for central workflows.
- Use [Advertising](../guides/advertising/) for broadcaster/peripheral workflows.
