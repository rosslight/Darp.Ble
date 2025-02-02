using Darp.Ble.Hci.Payload.Att;
using FluentAssertions;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttWriteReqTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttWriteReq.ExpectedOpCode.Should().HaveValue(0x12);
    }

    [Theory]
    [InlineData(25, "AABBCCDD", "121900AABBCCDD")]
    public void TryWriteLittleEndian_ShouldBeValid(
        ushort handle,
        string valueHexBytes,
        string expectedHexBytes
    )
    {
        var buffer = new byte[7];
        byte[] valueBytes = Convert.FromHexString(valueHexBytes);
        var value = new AttWriteReq { Handle = handle, Value = valueBytes };

        bool success = value.TryWriteLittleEndian(buffer);

        value.OpCode.Should().Be(AttOpCode.ATT_WRITE_REQ);
        value.GetByteCount().Should().Be(3 + valueBytes.Length);
        success.Should().BeTrue();
        Convert.ToHexString(buffer).Should().Be(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        var buffer = new byte[2];
        AttWriteReq value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.Should().BeFalse();
    }
}
