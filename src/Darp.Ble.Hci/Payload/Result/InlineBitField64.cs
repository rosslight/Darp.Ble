using System.Runtime.CompilerServices;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> A 64 bit wide bitfield </summary>
[InlineArray(8)]
public record struct InlineBitField64
{
    private byte _element0;
}