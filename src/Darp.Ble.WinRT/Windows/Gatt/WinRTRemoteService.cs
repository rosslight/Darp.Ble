using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.WinRT.Connections;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Darp.Ble.WinRT.Gatt;

internal sealed class WinRTRemoteService(WinRTConnectionDriver connection, GattDeviceService service)
    : IRemoteServiceDriver,
        IDisposable
{
#pragma warning disable CA2213 // The connection owns its services, not the other way round
    private readonly WinRTConnectionDriver _connection = connection;
#pragma warning restore CA2213
    private readonly GattDeviceService _service = service;

    public BleUuid Uuid { get; } = BleUuid.FromGuid(service.Uuid, inferType: true);

    /// <summary> One Windows call; Windows reads the pages itself, so an abandoned request cannot stop between them </summary>
    public async Task<IReadOnlyList<IRemoteCharacteristicDriver>> DiscoverCharacteristicsAsync(GattRequest request)
    {
        GattCharacteristicsResult result = await NativeCall
            .RunAsync(request, () => _service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached))
            .ConfigureAwait(false);
        NativeCall.ThrowIfFailed(result.Status, result.ProtocolError, "discover characteristics");
        return [.. result.Characteristics.Select(_connection.GetCharacteristic)];
    }

    public void Dispose() => _service.Dispose();
}
