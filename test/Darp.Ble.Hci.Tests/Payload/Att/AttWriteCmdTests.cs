using Darp.Ble.Hci.Payload.Att;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Att;

public sealed class AttWriteCmdTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        AttWriteCmd.ExpectedOpCode.ShouldHaveValue(0x52);
    }

    [Theory]
    [InlineData(25, "AABBCCDD", "521900AABBCCDD")]
    public void TryWriteLittleEndian_ShouldBeValid(ushort handle, string valueHexBytes, string expectedHexBytes)
    {
        var buffer = new byte[7];
        byte[] valueBytes = Convert.FromHexString(valueHexBytes);
        var value = new AttWriteCmd { Handle = handle, Value = valueBytes };

        bool success = value.TryWriteLittleEndian(buffer);

        value.OpCode.ShouldBe(AttOpCode.ATT_WRITE_CMD);
        value.GetByteCount().ShouldBe(3 + valueBytes.Length);
        success.ShouldBeTrue();
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        var buffer = new byte[2];
        AttWriteCmd value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
