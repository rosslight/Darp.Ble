# WinRT

`Darp.Ble.WinRT` wraps the default Windows Bluetooth adapter through WinRT APIs.

## Install

```powershell
dotnet add package Darp.Ble
dotnet add package Darp.Ble.WinRT
```

## Requirements

- Real enumeration requires a Windows target such as `net9.0-windows10.0.19041.0`.
- `InitializeAsync()` returns `DeviceVersionUnsupported` when the default adapter does not support Bluetooth Low Energy.
- `SetRandomAddressAsync()` is not supported by this backend.

!!! warning "Non-Windows targets"

    The `net9.0` target keeps the public registration API, but `WinBleFactory` yields no device outside Windows.

!!! info "Adapter-dependent roles"

    WinRT role support comes from Windows adapter capability flags. Check `IBleDevice.Capabilities` after initialization before using role-specific APIs.

## Registration

```csharp
using Darp.Ble;
using Darp.Ble.WinRT;

var manager = new BleManagerBuilder()
    .AddWinRT()
    .CreateManager();
```

`AddWinRT()` registers `WinBleFactory`, which yields the default Windows BLE device.

## Capabilities

WinRT maps Windows adapter flags to Darp.Ble roles:

| Windows adapter check | Darp.Ble role |
| --- | --- |
| `IsLowEnergySupported` | `Observer` |
| `IsCentralRoleSupported` | `Central` |
| `IsAdvertisementOffloadSupported` | `Broadcaster` |
| `IsPeripheralRoleSupported` | `Peripheral` |

## Next steps

- Use [Scan advertisements](../../guides/scan-advertisements/) to observe advertisements.
- Use [Connect as a central](../../guides/connect-as-central/) when the initialized device supports the central role.
- Use [Broadcast advertisements](../../guides/broadcast-advertisements/) when the initialized device supports the broadcaster role.
- Use [Accept connections](../../guides/accept-connections/) when the initialized device supports the peripheral role.
