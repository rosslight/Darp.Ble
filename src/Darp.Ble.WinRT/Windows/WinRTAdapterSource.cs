using Darp.Ble.Backends;
using Windows.Devices.Bluetooth;
using Windows.Devices.Radios;

namespace Darp.Ble.WinRT;

/// <summary> The system's default Bluetooth adapter </summary>
internal sealed class WinRTAdapterSource : IBleAdapterSource
{
    public string? Name => "Windows";

    public async Task<IBleAdapterDriver> OpenAsync(BleBackendContext context, CancellationToken cancellationToken)
    {
        BluetoothAdapter? adapter = await BluetoothAdapter
            .GetDefaultAsync()
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (adapter is null)
            throw new BleOpenException(BleOpenErrorKind.NotAvailable, "Windows has no Bluetooth adapter");
        if (!adapter.IsLowEnergySupported)
            throw new BleOpenException(
                BleOpenErrorKind.NotAvailable,
                "The Bluetooth adapter does not support Low Energy"
            );
        Radio? radio = await adapter.GetRadioAsync().AsTask(cancellationToken).ConfigureAwait(false);
        if (radio is not { State: RadioState.On })
        {
            throw new BleOpenException(
                BleOpenErrorKind.Disabled,
                $"The Bluetooth radio is {radio?.State.ToString() ?? "unavailable"}",
                isTransient: true
            );
        }
        return new WinRTAdapterDriver(adapter, context);
    }
}
