namespace Darp.Ble.Hci;

/// <summary> General settings applied to the <see cref="HciDevice"/> </summary>
/// <param name="DefaultHciCommandTimeoutMs"> The number of milliseconds to wait until a HCI command times out </param>
/// <param name="DefaultAttTimeoutMs"> The number of milliseconds to wait until an ATT request times out </param>
public sealed record HciSettings(int DefaultHciCommandTimeoutMs = 5000, int DefaultAttTimeoutMs = 30000)
{
    /// <summary> The default settings to be used </summary>
    public static readonly HciSettings Default = new();
}
