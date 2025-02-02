using System.Runtime.CompilerServices;

namespace Darp.Ble.Hci.Payload.Result;

/// <summary> A 64 byte wide bitfield </summary>
[InlineArray(64)]
public struct InlineBitField64
{
    private byte _element0;
}