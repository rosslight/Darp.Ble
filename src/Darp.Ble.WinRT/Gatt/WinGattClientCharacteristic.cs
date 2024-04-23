using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.WinRT.Gatt;

internal sealed class WinGattClientCharacteristic(
    WinGattClientService winService,
    GattLocalCharacteristic winCharacteristic)
    : GattClientCharacteristic(new BleUuid(winCharacteristic.Uuid, inferType: true), (GattProperty)winCharacteristic.CharacteristicProperties)
{
    public WinGattClientService WinService { get; } = winService;
    private readonly GattLocalCharacteristic _winCharacteristic = winCharacteristic;

    /// <inheritdoc />
    protected override IDisposable OnWriteCore(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback)
    {
        return Observable.FromEventPattern<TypedEventHandler<GattLocalCharacteristic, GattWriteRequestedEventArgs>,
                GattLocalCharacteristic, GattWriteRequestedEventArgs>(
                addHandler => _winCharacteristic.WriteRequested += addHandler,
                removeHandler => _winCharacteristic.WriteRequested -= removeHandler)
            .Select(pattern => Observable.FromAsync(async token =>
            {
                using Deferral deferral = pattern.EventArgs.GetDeferral();
                GattWriteRequest request = await pattern.EventArgs.GetRequestAsync().AsTask(token);
                IGattClientPeer peerClient = WinService.Peripheral.GetOrRegisterSession(pattern.EventArgs.Session);
                DataReader reader = DataReader.FromBuffer(request.Value);
                byte[] bytes = reader.DetachBuffer().ToArray();
                GattProtocolStatus status = await callback(peerClient, bytes, token);
                if (request.Option == GattWriteOption.WriteWithResponse)
                {
                    if (status is GattProtocolStatus.Success)
                        request.Respond();
                    else
                        request.RespondWithProtocolError((byte)status);
                }
            }))
            .Concat()
            .Subscribe();
    }

    /// <inheritdoc />
    protected override async Task<bool> NotifyAsyncCore(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken)
    {
        GattSubscribedClient? subscribedClient = _winCharacteristic.SubscribedClients
            .FirstOrDefault(x => string.Equals(x.Session.DeviceId.Id, clientPeer.Address.ToString(), StringComparison.OrdinalIgnoreCase));
        if (subscribedClient is null) return false;

        GattClientNotificationResult result = await _winCharacteristic.NotifyValueAsync(source.AsBuffer(), subscribedClient).AsTask(cancellationToken);
        return result.Status is GattCommunicationStatus.Success;
    }
}