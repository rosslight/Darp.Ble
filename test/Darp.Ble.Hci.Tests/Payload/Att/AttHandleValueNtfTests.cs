using Darp.Ble.Hci.Payload.Att;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttHandleValueNtfTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttHandleValueNtf.ExpectedOpCode.Should().HaveValue(0x1B);
    }

    [Theory]
    [InlineData("1B2100", 0x0021, "")]
    [InlineData("1B2100AABBCCDDEEFF", 0x0021, "AABBCCDDEEFF")]
    public void TryReadLittleEndian_ShouldBeValid(string hexBytes, ushort handle, string valueHexBytes)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        byte[] valueBytes = Convert.FromHexString(valueHexBytes);

        bool success = AttHandleValueNtf.TryReadLittleEndian(bytes, out AttHandleValueNtf value, out int decoded);

        success.Should().BeTrue();
        decoded.Should().Be(3 + valueBytes.Length);
        value.OpCode.Should().Be(AttOpCode.ATT_HANDLE_VALUE_NTF);
        value.Handle.Should().Be(handle);
        value.Value.ToArray().Should().BeEquivalentTo(valueBytes);
    }

    [Theory]
    [InlineData("", 0)]
    // [InlineData("1A2100", 0)] TODO: Handle parsing of invalid opCodes
    [InlineData("1B21", 0)]
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = AttHandleValueNtf.TryReadLittleEndian(bytes, out _, out int decoded);

        success.Should().BeFalse();
        decoded.Should().Be(expectedBytesDecoded);
    }
}