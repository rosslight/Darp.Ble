using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Payload.Command;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct DeviceAddress(byte byte0, byte byte1, byte byte2, byte byte3, byte byte4, byte byte5)
    : IDecodable<DeviceAddress>
{
    private readonly byte _byte0 = byte0;
    private readonly byte _byte1 = byte1;
    private readonly byte _byte2 = byte2;
    private readonly byte _byte3 = byte3;
    private readonly byte _byte4 = byte4;
    private readonly byte _byte5 = byte5;

    public ulong Address => _byte0 | (ulong)_byte1 << 8 | (ulong)_byte2 << 16
                            | (ulong)_byte3 << 24 | (ulong)_byte4 << 32 | (ulong)_byte5 << 40;

    public static implicit operator DeviceAddress(long value) => (ulong)value;
    public static implicit operator DeviceAddress(ulong value)
    {
        unsafe
        {
            ulong* ptr = &value;
            return Unsafe.Read<DeviceAddress>(ptr);
        }
    }

    public static implicit operator ulong(DeviceAddress address) => address.Address;

    public static DeviceAddress CreateRandomStatic(int? seed = null)
    {
        Random random = seed is null ? new Random() : new Random(seed.Value);
        long nextValue = random.NextInt64(0, 1L << 48);
        // Random static
        nextValue |= 0b11000000_00000000_00000000_00000000_00000000_00000000;
        return nextValue;
    }

    public static bool TryDecode(in ReadOnlyMemory<byte> source, out DeviceAddress result, out int bytesRead)
    {
        bytesRead = 6;
        result = default;
        if (source.Length < 6) return false;
        ReadOnlySpan<byte> span = source.Span;
        result = new DeviceAddress(span[0], span[1], span[2], span[3], span[4], span[5]);
        return true;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeSetRandomAddressCommand(DeviceAddress RandomAddress)
    : IHciCommand<HciLeSetRandomAddressCommand>
{
    public static HciOpCode OpCode => HciOpCode.HCI_LE_Set_Random_Address;
    public HciLeSetRandomAddressCommand GetThis() => this;
}