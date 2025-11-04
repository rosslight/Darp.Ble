namespace Darp.Ble.Hci.Host;

public sealed partial class GattClient
{
    private readonly AclConnection _connection;

    public GattClient(AclConnection connection)
    {
        _connection = connection;
    }
}
