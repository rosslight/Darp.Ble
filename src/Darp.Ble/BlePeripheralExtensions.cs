using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble;

/// <summary> Extensions for IBlePeripheral </summary>
public static class BlePeripheralExtensions
{
    /// <inheritdoc cref="IBlePeripheral.AddServiceAsync"/>
    public static Task<IGattClientService> AddServiceAsync(this IBlePeripheral peripheral,
        ushort uuid,
        CancellationToken cancellationToken = default)
    {
        return peripheral.AddServiceAsync(new BleUuid(uuid), cancellationToken);
    }
}