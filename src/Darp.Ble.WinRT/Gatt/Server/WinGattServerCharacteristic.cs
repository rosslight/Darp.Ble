using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT.Gatt.Server;

internal sealed class WinGattServerCharacteristic(GattCharacteristic gattCharacteristic, ILogger? logger)
    : GattServerCharacteristic(new BleUuid(gattCharacteristic.Uuid, inferType: true), logger)
{
    private readonly GattCharacteristic _gattCharacteristic = gattCharacteristic;
    private readonly ILogger? _logger = logger;

    /// <inheritdoc />
    protected override async Task WriteAsyncCore(byte[] bytes, CancellationToken cancellationToken)
    {
        var result = await _gattCharacteristic.WriteValueWithResultAsync(bytes.AsBuffer())
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (result.Status is not GattCommunicationStatus.Success)
            throw new Exception("Could not write");
    }

    protected override async Task<IDisposable> EnableNotificationsAsync<TState>(TState state,
        Action<TState, byte[]> onNotify,
        CancellationToken cancellationToken)
    {
        if (!_gattCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
        {
            throw new Exception("Characteristic does not support notification");
        }
        GattCommunicationStatus res = await _gattCharacteristic
            .WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (res is not GattCommunicationStatus.Success)
        {
            throw new Exception("Could not write notification status to cccd");
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