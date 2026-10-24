namespace Darp.Ble.Connections;

/// <summary> How a connection runs </summary>
public sealed record BleConnectionParameters
{
    /// <summary> How often the devices exchange packets. The backend rounds it to the controller's 1.25 ms units </summary>
    public TimeSpan ConnectionInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    internal void Validate(string parameterName)
    {
        // The Bluetooth range: 7.5 ms to 4 s
        if (ConnectionInterval < TimeSpan.FromMilliseconds(7.5) || ConnectionInterval > TimeSpan.FromSeconds(4))
            throw new ArgumentException("The connection interval must be between 7.5 ms and 4 s", parameterName);
    }
}
