using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT.Gatt.Server;

internal sealed class WinGattServerCharacteristic(GattCharacteristic gattCharacteristic, ILogger? logger)
    : GattServerCharacteristic(new BleUuid(gattCharacteristic.Uuid, inferType: true))
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

    protected override IConnectableObservable<byte[]> OnNotifyCore()
    {
        if (!_gattCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
        {
            return Observable.Throw<byte[]>(new Exception("Characteristic does not support notification")).Publish();
        }
        return Observable.Create<byte[]>(async (observer, token) =>
        {
            GattCommunicationStatus res = await _gattCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify)
                .AsTask(token)
                .ConfigureAwait(false);
            if (res is not GattCommunicationStatus.Success)
            {
                observer.OnError(new Exception("Could not write notification status to cccd"));
                return Disposable.Empty;
            }
            IDisposable disposable = Observable
                .FromEventPattern<TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>, GattCharacteristic, GattValueChangedEventArgs>(
                    addHandler => _gattCharacteristic.ValueChanged += addHandler,
                    removeHandler => _gattCharacteristic.ValueChanged -= removeHandler)
                .Select(x => x.EventArgs.CharacteristicValue.ToArray())
                .Where(x => x is not null)
                .Subscribe(observer);
            _logger?.LogTrace("Enabled notifications on {@Characteristic}", this);
            return disposable;
        }).Publish();
    }
}