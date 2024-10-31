using Darp.Ble.Data;
using Darp.Ble.Gap;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Implementation;

/// <summary> The broadcaster view of a ble device </summary>
public abstract class BleBroadcaster(ILogger? logger) : IBleBroadcaster
{
    /// <summary> The logger </summary>
    protected ILogger? Logger { get; } = logger;

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
    public IDisposable Advertise(AdvertisingData data, TimeSpan interval, AdvertisingParameters? parameters)
    {
        return AdvertiseCore(data, interval, parameters);
    }

    /// <inheritdoc cref="Advertise(AdvertisingData, TimeSpan, AdvertisingParameters)"/>
    protected abstract IDisposable AdvertiseCore(AdvertisingData data, TimeSpan interval, AdvertisingParameters? parameters);

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
        DisposeCore();
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void DisposeCore()
    {
        StopAll();
    }
}