using Darp.Ble.Data;

namespace Darp.Ble.Implementation;

/// <summary> The ble device implementation </summary>
public interface IBleDeviceImplementation
{
    /// <summary> Initializes the ble device. </summary>
    /// <returns> The status of the initialization. Success or a custom error code. </returns>
    Task<InitializeResult> InitializeAsync();
    /// <summary> Get access to the implementation specific observer </summary>
    IBleObserverImplementation? Observer { get; }
    /// <summary> Get an implementation specific identification string </summary>
    string Identifier { get; }
}