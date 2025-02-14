namespace Darp.Ble.Hci.Reactive;

/// <summary> Defines a provider for push-based notification and allows for ref types. </summary>
/// <typeparam name="T"> The object that provides notification information. </typeparam>
public interface IRefObservable<out T>
    where T : allows ref struct
{
    /// <summary> Notifies the provider that an observer is to receive notifications. </summary>
    /// <param name="observer"> The object that is to receive notifications. </param>
    /// <returns> A reference to an interface that allows observers to stop receiving notifications before the provider has finished sending them. </returns>
    IDisposable Subscribe(IRefObserver<T> observer);
}
