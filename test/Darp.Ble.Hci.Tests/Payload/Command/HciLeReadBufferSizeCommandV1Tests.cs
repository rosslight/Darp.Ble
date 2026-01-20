using Darp.Ble.Hci.Payload.Command;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeReadBufferSizeCommandV1Tests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeReadBufferSizeCommandV1.OpCode.ShouldHaveValue(0x0002 | (0x08 << 10));
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeValid()
    {
        byte[] buffer = [];
        var value = new HciLeReadBufferSizeCommandV1();

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeTrue();
        value.GetByteCount().ShouldBe(0);
    }
}
