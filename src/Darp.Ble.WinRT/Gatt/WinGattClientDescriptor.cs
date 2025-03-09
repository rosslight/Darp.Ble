using System.Runtime.InteropServices.WindowsRuntime;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Darp.Ble.WinRT.Gatt;

internal sealed class WinGattClientDescriptor : GattClientDescriptor
{
    public WinGattClientDescriptor(
        WinGattClientCharacteristic clientCharacteristic,
        GattLocalDescriptor winDescriptor,
        IGattCharacteristicValue value
    )
        : base(clientCharacteristic, value)
    {
        winDescriptor.ReadRequested += async (_, args) =>
        {
            using Deferral deferral = args.GetDeferral();
            GattReadRequest? request = await args.GetRequestAsync().AsTask().ConfigureAwait(false);
            try
            {
                IGattClientPeer peerClient = clientCharacteristic.Service.Peripheral.GetOrRegisterSession(args.Session);
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
                IGattClientPeer peerClient = clientCharacteristic.Service.Peripheral.GetOrRegisterSession(args.Session);
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
}
