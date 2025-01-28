using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT.Gatt.Server;

internal sealed class WinGattServerCharacteristic(GattServerService service, GattCharacteristic gattCharacteristic, ILogger<WinGattServerCharacteristic> logger)
    : GattServerCharacteristic(service, gattCharacteristic.AttributeHandle, new BleUuid(gattCharacteristic.Uuid, inferType: true), (GattProperty)gattCharacteristic.CharacteristicProperties, logger)
{
    private readonly GattCharacteristic _gattCharacteristic = gattCharacteristic;

    /// <inheritdoc />
    protected override async Task WriteAsyncCore(byte[] bytes, CancellationToken cancellationToken)
    {
        GattWriteResult result = await _gattCharacteristic.WriteValueWithResultAsync(bytes.AsBuffer(), GattWriteOption.WriteWithResponse)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (result.Status is GattCommunicationStatus.Success)
            return;
        if (result.Status is GattCommunicationStatus.ProtocolError)
            throw new GattCharacteristicException(this, $"Could not write because of protocol error {result.ProtocolError}");
        throw new GattCharacteristicException(this, $"Could not write because of {result.Status}");
    }

    protected override void WriteWithoutResponseCore(byte[] bytes)
    {
        _ = Task.Run(() => _gattCharacteristic.WriteValueAsync(bytes.AsBuffer(), GattWriteOption.WriteWithoutResponse));
    }

    protected override async Task<byte[]> ReadAsyncCore(CancellationToken cancellationToken)
    {
        GattReadResult result = await _gattCharacteristic.ReadValueAsync(BluetoothCacheMode.Uncached)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (result.Status is not GattCommunicationStatus.Success)
            throw new Exception($"Could not read value because of {result.Status} ({result.ProtocolError})");
        return result.Value.ToArray();
    }

    protected override async Task<IDisposable> EnableNotificationsAsync<TState>(TState state,
        Action<TState, byte[]> onNotify,
        CancellationToken cancellationToken)
    {
        if (!_gattCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
        {
            throw new GattCharacteristicException(this, "Characteristic does not support notification");
        }
        GattCommunicationStatus res = await _gattCharacteristic
            .WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (res is not GattCommunicationStatus.Success)
        {
            throw new GattCharacteristicException(this, "Could not write notification status to cccd");
        }

        return Observable
            .FromEventPattern<TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>, GattCharacteristic, GattValueChangedEventArgs>(
                addHandler => _gattCharacteristic.ValueChanged += addHandler,
                removeHandler => _gattCharacteristic.ValueChanged -= removeHandler)
            .Select(x => x.EventArgs.CharacteristicValue.ToArray())
            .Where(x => x is not null)
            .Subscribe(bytes => onNotify(state, bytes));
    }

    protected override async Task DisableNotificationsAsync()
    {
        await _gattCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
    }
}