using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using BitsKit;
using BitsKit.BitFields;
using Darp.Ble.Hci.Payload.Att;
using StructPacker;

namespace Darp.Ble.Hci.Tests;
[Pack]
internal ref struct ChatMsg
{
    public int ID { get; set; }
    public bool IsOutgoing { get; set; }
    public string Text { get; set; }
    public DateTime TimePosted { get; set; }

    [SkipPack]
    public string ExampleIgnoredProperty { get; set; }
}

public interface IPackable
{
    int GetPackSize();
    bool TryPack(Span<byte> destination);
}

public interface IUnpackable<T>
    where T : allows ref struct
{
    static abstract bool TryUnpack(ReadOnlySpan<byte> source, out T value);
    static abstract bool TryUnpack(ReadOnlySpan<byte> source, out T value, out int numberOfBytesRead);
}

[Pack, Unpack]
public readonly ref partial struct Test1()
{
    public static AttOpCode OpCode => AttOpCode.ATT_FIND_BY_TYPE_VALUE_REQ;
    private readonly AttOpCode _opCode = OpCode;
    public required ushort StartingHandle { get; init; }
    public required ushort EndingHandle { get; init; }
    public required ushort AttributeType { get; init; }
    public required ushort TestLength { get; init; }
    [PackLength(nameof(TestLength))] public required Span<byte> AttributeValue { get; init; }
}

public readonly ref partial struct Test1 : IPackable, IUnpackable<Test1>
{
    public readonly int GetPackSize() => 9 + TestLength;
    public readonly bool TryPack(Span<byte> destination)
    {
        
    }

    public static bool TryUnpack(ReadOnlySpan<byte> source, out Test1 value) => TryUnpack(source, out value, out _);

    public static bool TryUnpack(ReadOnlySpan<byte> source, out Test1 value, out int numberOfBytesRead)
    {
        
    }
}
public sealed class UnitTest1
{
    public static bool TryWrite<T>(Span<byte> destination, in T value)
        where T : struct, allows ref struct
    {
        if (Unsafe.SizeOf<T>() > (uint)destination.Length)
        {
            return false;
        }
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
        return true;
    }

    [Fact]
    public void Test1()
    {
        //create a test message
        var sourceMsg = new ChatMsg
        {
            ID = 5,
            IsOutgoing = true,
            Text = "Test",
            TimePosted = DateTime.MaxValue
        };

//save it to byte array
        byte[] byteArr = sourceMsg.Pack();
        Tools.PackMsgToArray()
        var bytes = Convert.FromHexString("0028FFFF4C4D");
        var testi = new Test1
        {
            StartingHandle = 1,
            EndingHandle = 0xFFFF,
            AttributeType = 123,
            AttributeValue = [0x4C, 0xFD],
        };
        var x =new ChatMsg2().Pack();
        Span<byte> span = stackalloc byte[9];
        bool x = TryWrite(span, testi);
        int i = 0;
    }
}