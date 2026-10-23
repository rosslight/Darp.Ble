namespace Darp.Ble.Hci;

/// <summary> General settings applied to the <see cref="HciDevice"/> </summary>
public sealed record HciSettings
{
    /// <summary> The default settings to be used </summary>
    public static HciSettings Default { get; } = new();

    /// <summary> How long to wait for the controller to answer a command, including the wait for the command slot. Default: 5 s </summary>
    public TimeSpan CommandTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
