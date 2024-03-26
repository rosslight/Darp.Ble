using Darp.Ble.Data;

namespace Darp.Ble.Implementation;

/// <summary> The ble device implementation </summary>
public interface IPlatformSpecificBleDevice : IDisposable
{
    /// <summary> Get an platform specific identification string </summary>
    string Identifier { get; }

    /// <summary> Get access to the platform specific observer </summary>
    IPlatformSpecificBleObserver? Observer { get; }
    /// <summary> Get access to the platform specific central </summary>
    object? Central { get; }

    /// <summary> Initializes the ble device. </summary>
    /// <returns> The status of the initialization. Success or a custom error code. </returns>
    Task<InitializeResult> InitializeAsync();
}