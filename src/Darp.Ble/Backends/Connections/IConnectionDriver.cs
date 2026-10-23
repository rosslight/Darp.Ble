namespace Darp.Ble.Backends;

/// <summary> One connection to a peripheral. Disposing it ends this app's connection and must end every outstanding driver task </summary>
public interface IConnectionDriver : IAsyncDisposable;
