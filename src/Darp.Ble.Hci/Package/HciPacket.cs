namespace Darp.Ble.Hci.Package;

file static class Ex
{
    public static string ToHexString(this in ReadOnlyMemory<byte> memory) => Convert.ToHexString(memory.Span);
}