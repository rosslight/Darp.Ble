namespace Darp.Ble.Implementation;

public interface IBleImplementation
{
    IEnumerable<BleDevice> EnumerateAdapters();
}