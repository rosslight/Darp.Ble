using Darp.Ble.Data;

namespace Darp.Ble.Diagnostics;

/// <summary> A manager's diagnostics options, fixed when it was built </summary>
/// <param name="IncludeAddresses"> Whether logs and spans carry peer addresses </param>
internal sealed record DiagnosticsSettings(bool IncludeAddresses)
{
    /// <summary> What logs show instead of a peer address that the options leave out </summary>
    private const string Hidden = "<hidden>";

    public static DiagnosticsSettings Default { get; } = new(IncludeAddresses: true);

    /// <summary> A peer address as logs and spans may show it </summary>
    public object Peer(BleAddress address) => IncludeAddresses ? address : Hidden;

    /// <summary> An exception of a peer operation as logs may attach it; its message may name the peer, so it is left out with the addresses </summary>
    public Exception? Exception(Exception exception) => IncludeAddresses ? exception : null;

    /// <summary> How a span describes a failure: the message, or only the exception's type when addresses are left out </summary>
    public string Describe(Exception exception) => IncludeAddresses ? exception.Message : exception.GetType().Name;
}
