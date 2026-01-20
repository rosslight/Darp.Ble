using Darp.Ble.Hci.Payload.Att;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttWriteReqTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttWriteReq.ExpectedOpCode.ShouldHaveValue(0x12);
    }

    [Theory]
    [InlineData(25, "AABBCCDD", "121900AABBCCDD")]
    public void TryWriteLittleEndian_ShouldBeValid(ushort handle, string valueHexBytes, string expectedHexBytes)
    {
        var buffer = new byte[7];
        byte[] valueBytes = Convert.FromHexString(valueHexBytes);
        var value = new AttWriteReq { AttributeHandle = handle, AttributeValue = valueBytes };

        bool success = value.TryWriteLittleEndian(buffer);

        value.OpCode.ShouldBe(AttOpCode.ATT_WRITE_REQ);
        value.GetByteCount().ShouldBe(3 + valueBytes.Length);
        success.ShouldBeTrue();
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        var buffer = new byte[2];
        AttWriteReq value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
