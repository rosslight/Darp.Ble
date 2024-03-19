namespace Darp.Ble.Data;

/// <summary> The result codes for initialization </summary>
public enum InitializeResult
{
    /// <summary> Initialization was successful </summary>
    Success = 0,
    /// <summary> Could not initialize because init was already running </summary>
    AlreadyInitializing,
    /// <summary> The adapter is not available. </summary>
    AdapterNotAvailable
}