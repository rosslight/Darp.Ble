using Darp.Ble.Hci.Payload.Command;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeReadLocalSupportedFeaturesCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeReadLocalSupportedFeaturesCommand.OpCode.ShouldHaveValue(0x0003 | (0x08 << 10));
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeValid()
    {
        byte[] buffer = [];
        var value = new HciLeReadLocalSupportedFeaturesCommand();

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeTrue();
        value.GetByteCount().ShouldBe(0);
    }
}
