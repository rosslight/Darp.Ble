namespace Darp.Ble.Implementation;

/// <summary> The ble implementation config </summary>
public interface IBleFactory
{
    /// <summary> Enumerate all adapters which can be found by the implementation </summary>
    /// <returns> All implementation specific ble devices </returns>
    IEnumerable<IBleDeviceImplementation> EnumerateAdapters();
}