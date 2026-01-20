using System.Buffers.Binary;
using Darp.Ble.Hci.Payload.Att;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttFindInformationRspTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttFindInformationRsp.ExpectedOpCode.ShouldHaveValue(0x05);
    }

    [Theory]
    [InlineData("0501190061FF", 0x0019, 0xFF61)]
    [InlineData(
        "0501190061FF1A0001291B0003281C0062FF1D0002291E000129",
        0x0019,
        0xFF61,
        0x001A,
        0x2901,
        0x001B,
        0x2803,
        0x001C,
        0xFF62,
        0x001D,
        0x2902,
        0x001E,
        0x2901
    )]
    public void TryReadLittleEndian_16BitData_ShouldBeValid(string hexBytes, params int[] informationData)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        AttFindInformationData[] findInformationData = informationData
            .Pairs()
            .Select(x =>
            {
                var uuidBuffer = new byte[2];
                BinaryPrimitives.WriteUInt16LittleEndian(uuidBuffer, (ushort)x.Second);
                return new AttFindInformationData { Handle = (ushort)x.First, Uuid = uuidBuffer };
            })
            .ToArray();

        bool success = AttFindInformationRsp.TryReadLittleEndian(
            bytes,
            out AttFindInformationRsp value,
            out int decoded
        );

        success.ShouldBeTrue();
        decoded.ShouldBe(2 + 4 * findInformationData.Length);
        value.OpCode.ShouldBe(AttOpCode.ATT_FIND_INFORMATION_RSP);
        value.Format.ShouldBe(AttFindInformationFormat.HandleAnd16BitUuid);
        value
            .InformationData.ToArray()
            .Zip(findInformationData)
            .ShouldAllSatisfy(x =>
            {
                x.First.Handle.ShouldBe(x.Second.Handle);
                x.First.Uuid.ToArray().ShouldBe(x.Second.Uuid.ToArray());
            });
    }

    [Theory]
    [InlineData("050219000000FFE000001000800000805F9B34FB", 0x0019, "0000FFE000001000800000805F9B34FB")]
    public void TryReadLittleEndian_128BitData_ShouldBeValid(string hexBytes, ushort handle, string guidHexString)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        byte[] uuidBytes = Convert.FromHexString(guidHexString);
        AttFindInformationData[] findInformationData = [new(handle, uuidBytes)];

        bool success = AttFindInformationRsp.TryReadLittleEndian(
            bytes,
            out AttFindInformationRsp value,
            out int decoded
        );

        success.ShouldBeTrue();
        decoded.ShouldBe(2 + 18 * findInformationData.Length);
        value.OpCode.ShouldBe(AttOpCode.ATT_FIND_INFORMATION_RSP);
        value.Format.ShouldBe(AttFindInformationFormat.HandleAnd128BitUuid);

        value
            .InformationData.ToArray()
            .Zip(findInformationData)
            .ShouldAllSatisfy(x =>
            {
                x.First.Handle.ShouldBe(x.Second.Handle);
                x.First.Uuid.ToArray().ShouldBe(x.Second.Uuid.ToArray());
            });
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("0503190061FF", 0)]
    [InlineData("0401190061FF", 0)]
    [InlineData("0501190061FF00", 0)]
    [InlineData("0501190061", 0)]
    [InlineData("0501", 0)]
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttFindInformationRsp.TryReadLittleEndian(bytes, out _, out int decoded);

        success.ShouldBeFalse();
        decoded.ShouldBe(expectedBytesDecoded);
    }
}
