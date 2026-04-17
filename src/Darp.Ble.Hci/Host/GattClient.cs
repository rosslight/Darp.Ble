namespace Darp.Ble.Hci.Host;

/// <summary>
/// Provides GATT client operations for an ACL connection.
/// </summary>
public sealed partial class GattClient
{
    private readonly AclConnection _connection;

    /// <summary>
    /// Initializes a new GATT client bound to an ACL connection.
    /// </summary>
    /// <param name="connection">The connection used for GATT client traffic.</param>
    public GattClient(AclConnection connection)
    {
        _connection = connection;
    }
}
