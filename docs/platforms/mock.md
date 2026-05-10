# Mock

`Darp.Ble.Mock` is an in-process BLE simulation. Use it when tests need scan/connect flows without physical devices.

## Install

```powershell
dotnet add package Darp.Ble
dotnet add package Darp.Ble.Mock
```

## Requirements

- `AddPeripheral(...)` creates simulated remote peripherals that can advertise and host GATT data.
- `AddCentral(...)` currently does not add simulated central behavior.
- Set `BleMockFactory.Scheduler` when a test needs control over time.

!!! info "Mock device roles"

    The primary mock device returned by `BleManager` scans and connects with `Observer` and `Central`. Configured mock peripherals advertise and serve data with `Broadcaster` and `Peripheral`.

## Registration

Empty mock backend:

```csharp
using Darp.Ble;
using Darp.Ble.Mock;

var manager = new BleManagerBuilder()
    .AddMock()
    .CreateManager();
```

Mock backend with one configured peripheral:

```csharp
using Darp.Ble.Data;

var manager = new BleManagerBuilder()
    .AddMock(factory =>
    {
        factory.AddPeripheral(async device =>
        {
            await device.SetRandomAddressAsync(BleAddress.NewRandomStaticAddress());
        }, name: "Mock peripheral");
    })
    .CreateManager();
```

## Capabilities

| Device | Roles |
| --- | --- |
| Primary mock device returned by `BleManager` | `Observer`, `Central` |
| Configured mock peripherals | `Broadcaster`, `Peripheral` |

## Next steps

- Use [Testing with mock](../../guides/testing-with-mock/) for test-focused patterns.
- Use [Scan advertisements](../../guides/scan-advertisements/) and [Connect as a central](../../guides/connect-as-central/) to exercise application flows against mocked devices.
