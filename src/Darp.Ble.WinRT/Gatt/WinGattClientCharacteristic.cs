using System.Runtime.InteropServices.WindowsRuntime;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;
using Darp.Ble.Gatt.Client;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Darp.Ble.WinRT.Gatt;

internal sealed class WinGattClientCharacteristic : GattClientCharacteristic
{
    public new WinGattClientService Service { get; }
    private readonly GattLocalCharacteristic _winCharacteristic;

    public WinGattClientCharacteristic(
        WinGattClientService winService,
        GattLocalCharacteristic winCharacteristic,
        IGattCharacteristicValue value,
        ILogger<WinGattClientCharacteristic> logger
    )
        : base(winService, (GattProperty)winCharacteristic.CharacteristicProperties, value, logger)
    {
        Service = winService;
        _winCharacteristic = winCharacteristic;
        winCharacteristic.ReadRequested += async (_, args) =>
        {
            using Deferral deferral = args.GetDeferral();
            GattReadRequest? request = await args.GetRequestAsync().AsTask().ConfigureAwait(false);
            try
            {
                IGattClientPeer peerClient = Service.Peripheral.GetOrRegisterSession(args.Session);
                byte[] readValue = await Value.ReadValueAsync(peerClient).ConfigureAwait(false);
                request.RespondWithValue(readValue.AsBuffer());
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
                IGattClientPeer peerClient = Service.Peripheral.GetOrRegisterSession(args.Session);
                DataReader reader = DataReader.FromBuffer(request.Value);
                byte[] bytes = reader.DetachBuffer().ToArray();
                GattProtocolStatus status = await Value.WriteValueAsync(peerClient, bytes).ConfigureAwait(false);
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

    protected override void OnDescriptorAdded(IGattCharacteristicValue value)
    {
        BleUuid uuid = value.AttributeType;
        GattLocalDescriptorResult result = _winCharacteristic
            .CreateDescriptorAsync(uuid.Value, new GattLocalDescriptorParameters())
            .GetResults();
        if (result.Error is not BluetoothError.Success)
            throw new Exception("Could not add descriptor to windows");
        var winDescriptor = result.Descriptor;
        winDescriptor.ReadRequested += async (_, args) =>
        {
            using Deferral deferral = args.GetDeferral();
            GattReadRequest? request = await args.GetRequestAsync().AsTask().ConfigureAwait(false);
            try
            {
                IGattClientPeer peerClient = Service.Peripheral.GetOrRegisterSession(args.Session);
                byte[] readValue = await Value.ReadValueAsync(peerClient).ConfigureAwait(false);
                request.RespondWithValue(readValue.AsBuffer());
            }
            catch
            {
                request.RespondWithProtocolError(123);
            }
        };
        winDescriptor.WriteRequested += async (_, args) =>
        {
            using Deferral deferral = args.GetDeferral();
            GattWriteRequest request = await args.GetRequestAsync().AsTask().ConfigureAwait(false);
            try
            {
                IGattClientPeer peerClient = Service.Peripheral.GetOrRegisterSession(args.Session);
                DataReader reader = DataReader.FromBuffer(request.Value);
                byte[] bytes = reader.DetachBuffer().ToArray();
                GattProtocolStatus status = await Value.WriteValueAsync(peerClient, bytes).ConfigureAwait(false);
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

    protected override async ValueTask NotifyAsyncCore(IGattClientPeer clientPeer, byte[] value)
    {
        GattSubscribedClient? subscribedClient = _winCharacteristic.SubscribedClients.FirstOrDefault(x =>
        {
            BleAddress address = BleAddress.Parse(x.Session.DeviceId.Id[^17..], provider: null);
            return address == clientPeer.Address;
        });
        if (subscribedClient is null)
            return;

        await _winCharacteristic
            .NotifyValueAsync(value.AsBuffer(), subscribedClient)
            .AsTask(CancellationToken.None)
            .ConfigureAwait(false);
    }

    protected override async Task IndicateAsyncCore(
        IGattClientPeer clientPeer,
        byte[] value,
        CancellationToken cancellationToken
    )
    {
        GattSubscribedClient? subscribedClient = _winCharacteristic.SubscribedClients.FirstOrDefault(x =>
        {
            BleAddress address = BleAddress.Parse(x.Session.DeviceId.Id[^17..], provider: null);
            return address == clientPeer.Address;
        });
        if (subscribedClient is null)
            return;

        GattClientNotificationResult result = await _winCharacteristic
            .NotifyValueAsync(value.AsBuffer(), subscribedClient)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (result.Status is not GattCommunicationStatus.Success)
            throw new Exception("Hey thats not good :(");
    }
}
