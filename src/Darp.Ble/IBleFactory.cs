namespace Darp.Ble;

/// <summary> The ble implementation config </summary>
public interface IBleFactory
{
    /// <summary> Enumerate all adapters which can be found by the implementation </summary>
    /// <param name="serviceProvider"> The service provider </param>
    /// <returns> All implementation specific ble devices </returns>
    IEnumerable<IBleDevice> EnumerateDevices(IServiceProvider serviceProvider);
}
