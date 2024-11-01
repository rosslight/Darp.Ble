using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> Response to <see cref="HciLeReadLocalSupportedFeaturesCommand"/> </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeReadLocalSupportedFeaturesResult : IDefaultDecodable<HciLeReadLocalSupportedFeaturesResult>
{
    /// <summary> The <see cref="HciCommandStatus"/> </summary>
    public required HciCommandStatus Status { get; init; }
    /// <summary> Bit Mask List of page 0 of the supported LE features </summary>
    /// <remarks> https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/low-energy-controller/link-layer-specification.html#UUID-25d414b5-8c50-cd46-fd17-80f0f816f354 </remarks>
    public required ulong LeFeatures { get; init; }
}