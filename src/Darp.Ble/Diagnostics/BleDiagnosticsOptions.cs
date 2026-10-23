namespace Darp.Ble.Diagnostics;

/// <summary> What the library's logs and spans may contain, independent of the log level </summary>
public sealed class BleDiagnosticsOptions
{
    internal BleDiagnosticsOptions() { }

    /// <summary> Whether logs and spans carry the addresses of peers; on by default, as test and service tools need them </summary>
    public bool IncludeAddresses { get; set; } = true;
}
