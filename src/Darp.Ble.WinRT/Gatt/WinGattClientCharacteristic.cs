using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.WinRT.Gatt;

internal sealed class WinGattClientCharacteristic : GattClientCharacteristic
{
    public WinGattClientService WinService { get; }
    private readonly GattLocalCharacteristic _winCharacteristic;

    public WinGattClientCharacteristic(WinGattClientService winService,
        GattLocalCharacteristic winCharacteristic,
        IGattClientService.OnReadCallback? onRead,
        IGattClientService.OnWriteCallback? onWrite)
        : base(winService, BleUuid.FromGuid(winCharacteristic.Uuid, inferType: true), (GattProperty)winCharacteristic.CharacteristicProperties, onRead, onWrite)
    {
        WinService = winService;
        _winCharacteristic = winCharacteristic;
        winCharacteristic.ReadRequested += async (_, args) =>
        {
            using Deferral deferral = args.GetDeferral();
            GattReadRequest? request = await args.GetRequestAsync().AsTask().ConfigureAwait(false);
            try
            {
                IGattClientPeer peerClient = WinService.Peripheral.GetOrRegisterSession(args.Session);
                byte[] value = await GetValueAsync(peerClient, CancellationToken.None).ConfigureAwait(false);
                request.RespondWithValue(value.AsBuffer());
            }
            catch
            {
                request.RespondWithProtocolError(123);
            }
        };
        winCharacteristic.WriteRequested += async (_, args) =>
        {
            using Deferral deferral = args.GetDeferral();
            GattWriteRequest request = await args.GetRequestAsync().AsTask().ConfigureAwait(false);
            try
            {
                IGattClientPeer peerClient =
                    WinService.Peripheral.GetOrRegisterSession(args.Session);
                DataReader reader = DataReader.FromBuffer(request.Value);
                byte[] bytes = reader.DetachBuffer().ToArray();
                GattProtocolStatus status =
                    await UpdateValueAsync(peerClient, bytes, CancellationToken.None).ConfigureAwait(false);
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
        };
    }

    protected override void NotifyCore(IGattClientPeer clientPeer, byte[] value)
    {
        GattSubscribedClient? subscribedClient = _winCharacteristic.SubscribedClients
            .FirstOrDefault(x =>
            {
                BleAddress address = BleAddress.Parse(x.Session.DeviceId.Id[^17..], provider: null);
                return address == clientPeer.Address;
            });
        if (subscribedClient is null) return;

        _ = _winCharacteristic.NotifyValueAsync(value.AsBuffer(), subscribedClient).AsTask(CancellationToken.None);
    }

    protected override async Task IndicateAsyncCore(IGattClientPeer clientPeer, byte[] value, CancellationToken cancellationToken)
    {
        GattSubscribedClient? subscribedClient = _winCharacteristic.SubscribedClients
            .FirstOrDefault(x =>
            {
                BleAddress address = BleAddress.Parse(x.Session.DeviceId.Id[^17..], provider: null);
                return address == clientPeer.Address;
            });
        if (subscribedClient is null) return;

        GattClientNotificationResult result = await _winCharacteristic.NotifyValueAsync(value.AsBuffer(), subscribedClient).AsTask(cancellationToken).ConfigureAwait(false);
        if (result.Status is not GattCommunicationStatus.Success)
            throw new Exception("Hey thats not good :(");
    }
}