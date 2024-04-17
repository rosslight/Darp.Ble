using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble;

/// <summary> The ble broadcaster </summary>
public interface IBleBroadcaster : IAsyncDisposable
{
    /// <summary> Advertise a specific advertising set. Stop advertising using the disposable </summary>
    /// <param name="advertisingSet"> The <see cref="AdvertisingSet"/> to be advertised </param>
    /// <returns> A disposable which allows for stopping </returns>
    IDisposable Advertise(AdvertisingSet advertisingSet);
    /// <summary> Advertise an observable </summary>
    /// <param name="source"> The source which triggers an advertisement </param>
    /// <param name="parameters"> The parameters to be used </param>
    /// <returns> A disposable which allows for stopping </returns>
    IDisposable Advertise(IObservable<AdvertisingData> source, AdvertisingParameters? parameters = null);
    /// <summary> Stop all running advertisements </summary>
    void StopAll();
}