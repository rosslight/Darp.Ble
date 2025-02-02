using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT.Gatt.Server;

internal sealed class WinGattServerDescriptor(
    GattServerCharacteristic characteristic,
    GattDescriptor winDescriptor,
    ILogger<WinGattServerDescriptor> logger)
    : GattServerDescriptor(characteristic, BleUuid.FromGuid(winDescriptor.Uuid, inferType: true), logger)
{
    private readonly GattDescriptor _winDescriptor = winDescriptor;

    public override async Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
    {
        GattReadResult result = await _winDescriptor.ReadValueAsync()
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (result.Status is not GattCommunicationStatus.Success)
            throw new Exception($"Could not read value because of {result.Status} ({result.ProtocolError})");
        return result.Value.ToArray();
    }

    public override async Task<bool> WriteAsync(byte[] bytes, CancellationToken cancellationToken = default)
    {
        GattWriteResult result = await _winDescriptor.WriteValueWithResultAsync(bytes.AsBuffer())
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (result.Status is GattCommunicationStatus.Success)
            return true;
        if (result.Status is GattCommunicationStatus.ProtocolError)
            throw new Exception($"Could not write because of protocol error {result.ProtocolError}");
        throw new Exception($"Could not write because of {result.Status}");
    }
}