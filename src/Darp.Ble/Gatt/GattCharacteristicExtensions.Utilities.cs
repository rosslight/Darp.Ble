using System.Runtime.CompilerServices;
using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt;

public static partial class GattCharacteristicExtensions
{
    internal static Func<IGattClientPeer, PermissionCheckStatus> CreateReadAccessPermissionFunc(
        this IGattAttribute.OnReadAsyncCallback? nullable
    )
    {
        if (nullable is null)
            return _ => PermissionCheckStatus.ReadNotPermittedError;
        return _ => PermissionCheckStatus.Success;
    }

    internal static Func<IGattClientPeer, PermissionCheckStatus> CreateWriteAccessPermissionFunc(
        this IGattAttribute.OnWriteAsyncCallback? nullable
    )
    {
        if (nullable is null)
            return _ => PermissionCheckStatus.WriteNotPermittedError;
        return _ => PermissionCheckStatus.Success;
    }
}
