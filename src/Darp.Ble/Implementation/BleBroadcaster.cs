using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Implementation;

/// <summary> The broadcaster view of a ble device </summary>
public abstract class BleBroadcaster : IBleBroadcaster
{
    /// <inheritdoc />
    public IDisposable Advertise(AdvertisingSet advertisingSet) => throw new NotImplementedException();

    /// <inheritdoc />
    public IDisposable Advertise(IObservable<AdvertisingData> source, AdvertisingParameters? parameters = null)
    {
        return AdvertiseCore(source, parameters);
    }

    /// <summary> Core implementation of starting an advertisement broadcast </summary>
    /// <param name="source"> The source which triggers an advertisement </param>
    /// <param name="parameters"> The parameters to be used </param>
    /// <returns> A disposable which allows for stopping </returns>
    protected abstract IDisposable AdvertiseCore(IObservable<AdvertisingData> source, AdvertisingParameters? parameters);

    /// <inheritdoc />
    public void StopAll()
    {
        StopAllCore();
    }

    /// <summary> Core implementation of stopping all advertisements </summary>
    protected abstract void StopAllCore();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        StopAll();
        DisposeCore();
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void DisposeCore() { }
}