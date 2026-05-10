# Android

`Darp.Ble.Android` wraps Android's `BluetoothManager`. The current implementation is scanner-focused: it creates an observer role, not central/broadcaster/peripheral roles.

## Install

```powershell
dotnet add package Darp.Ble
dotnet add package Darp.Ble.Android
```

The package targets `net9.0-android` and sets Android 12 / API 31 as the supported OS platform baseline.

## Requirements

- `AndroidBleFactory` requires an Android `BluetoothManager`.
- The factory yields no device when the adapter is missing or the device has no BLE feature.
- `InitializeAsync()` returns `DeviceNotEnabled` when Bluetooth is off.
- `SetRandomAddressAsync()` is not supported by this backend.

Declare and request scan permission before initializing:

```xml
<uses-permission android:name="android.permission.BLUETOOTH_SCAN" />
```

The code still has a pre-API-31 fallback for coarse and fine location permissions, but the project baseline is API 31.

!!! warning "Missing scan permission"

    Initialization can still return `Success` without creating `Observer` when scan permission is missing or `BluetoothLeScanner` is unavailable. Check `IBleDevice.Capabilities` before using `device.Observer`.

## Registration

```csharp
using Android.Bluetooth;
using Darp.Ble;
using Darp.Ble.Android;

if (GetSystemService(BluetoothService) is BluetoothManager bluetoothManager)
{
    var manager = new BleManagerBuilder()
        .Add(new AndroidBleFactory(bluetoothManager))
        .CreateManager();
}
```

## Capabilities

| Role | Status |
| --- | --- |
| `Observer` | Created when BLE scanner and scan permission are available. |
| `Central` | Not initialized by the current Android backend. |
| `Broadcaster` | Not initialized by the current Android backend. |
| `Peripheral` | Not initialized by the current Android backend. |

## Next steps

- Use [Scanning](../../guides/scanning/) to observe advertisements.
- Review Android permissions before relying on scan results.
- Use another backend, such as [HciHost](../hci-host/), when you need central, broadcaster, or peripheral roles today.
