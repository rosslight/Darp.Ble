using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Gatt;

namespace Darp.Ble.HciHost;

public static class HciHostLoggingStrings
{
    public const string Name = "Darp.Ble.HciHost";
}

[SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
internal static class Logging
{
    private static readonly ActivitySource ActivitySource = new(HciHostLoggingStrings.Name);

    public static Activity? StartHandleAttRequestActivity<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TAttPdu
    >(TAttPdu request, HciHostGattClientPeer peer)
        where TAttPdu : IAttPdu, IBinaryWritable
    {
        Activity? activity = ActivitySource.StartActivity("Handle ATT request {Name}");
        if (activity is null)
            return activity;

        string requestName = request.OpCode.ToString().ToUpperInvariant();
        activity.SetTag("Name", requestName);
        activity.SetTag("DeviceAddress", peer.Peripheral.Device.RandomAddress.ToString());
        activity.SetTag("Connection.Handle", $"{peer.ConnectionHandle:X4}");
        activity.SetTag("Connection.ServerAddress", peer.Peripheral.Device.RandomAddress.ToString());
        activity.SetTag("Connection.ClientAddress", peer.Address.ToString());
        activity.SetTag("Connection.Role", "Server");

        activity.SetDeconstructedTags("Request", request, orderEntries: true);
        activity.SetTag("Request.OpCode", requestName);
        return activity;
    }
}
