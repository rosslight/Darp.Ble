namespace Darp.Ble.Hci.Reactive;

/// <summary> Provides a mechanism for receiving push-based notifications. Allows for ref types </summary>
/// <typeparam name="T"> The object that provides notification information. </typeparam>
public interface IRefObserver<in T>
    where T : allows ref struct
{
    /// <summary> Provides the observer with new data. </summary>
    /// <param name="value"> The current notification information. </param>
    void OnNext(T value);

    /// <summary> Notifies the observer that the provider has experienced an error condition. </summary>
    /// <param name="error"> An object that provides additional information about the error. </param>
    void OnError(Exception error);

    /// <summary> Notifies the observer that the provider has finished sending push-based notifications. </summary>
    void OnCompleted();
}
