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
        _winCharacteristic.WriteRequested += (sender, args) =>
        {
            int i = 0;
        };
        return Observable.FromEventPattern<TypedEventHandler<GattLocalCharacteristic, GattWriteRequestedEventArgs>,
                GattLocalCharacteristic, GattWriteRequestedEventArgs>(
                addHandler => _winCharacteristic.WriteRequested += addHandler,
                removeHandler => _winCharacteristic.WriteRequested -= removeHandler)
            .Subscribe(async pattern =>
            {
                using Deferral deferral = pattern.EventArgs.GetDeferral();
                GattWriteRequest request = await pattern.EventArgs.GetRequestAsync().AsTask();
                try
                {
                    IGattClientPeer peerClient =
                        WinService.Peripheral.GetOrRegisterSession(pattern.EventArgs.Session);
                    DataReader reader = DataReader.FromBuffer(request.Value);
                    byte[] bytes = reader.DetachBuffer().ToArray();
                    GattProtocolStatus status = await callback(peerClient, bytes, default);
                    if (request.Option == GattWriteOption.WriteWithResponse)
                    {
                        if (status is GattProtocolStatus.Success)
                            request.Respond();
                        else
                            request.RespondWithProtocolError((byte)status);
                    }
                }
                catch
                {
                    // ignored
                }
            });
    }

    /// <inheritdoc />
    protected override async Task<bool> NotifyAsyncCore(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken)
    {
        GattSubscribedClient? subscribedClient = _winCharacteristic.SubscribedClients
            .FirstOrDefault(x =>
            {
                BleAddress address = BleAddress.Parse(x.Session.DeviceId.Id[^17..], provider: null);
                return address == clientPeer.Address;
            });
        if (subscribedClient is null) return false;

        GattClientNotificationResult result = await _winCharacteristic.NotifyValueAsync(source.AsBuffer(), subscribedClient).AsTask(cancellationToken);
        return result.Status is GattCommunicationStatus.Success;
    }
}