using Darp.Ble.Gatt.Att;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt;

internal static class GattUtilities
{
    public static Func<IGattClientPeer, PermissionCheckStatus> CreateReadAccessPermissionFunc(
        this IGattAttribute.OnReadAsyncCallback? nullable
    )
    {
        if (nullable is null)
            return _ => PermissionCheckStatus.ReadNotPermittedError;
        return _ => PermissionCheckStatus.Success;
    }

    public static Func<IGattClientPeer, PermissionCheckStatus> CreateWriteAccessPermissionFunc(
        this IGattAttribute.OnWriteAsyncCallback? nullable
    )
    {
        if (nullable is null)
            return _ => PermissionCheckStatus.WriteNotPermittedError;
        return _ => PermissionCheckStatus.Success;
    }
}
