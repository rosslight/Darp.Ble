using System.Security.Cryptography;
using Darp.Ble.Hci.Payload.Command;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeSetRandomAddressCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeSetRandomAddressCommand.OpCode.ShouldHaveValue(0x0005 | (0x08 << 10));
    }

    [Theory]
    [InlineData(0xF5F4F3F2F1F0, "F0F1F2F3F4F5")]
    public void TryWriteLittleEndian_ShouldBeValid(ulong address, string expectedHexBytes)
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(6);
        var value = new HciLeSetRandomAddressCommand { RandomAddress = address };

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeTrue();
        value.GetByteCount().ShouldBe(6);
        value.RandomAddress.ShouldBe((UInt48)address);
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(5);
        HciLeSetExtendedScanParametersCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
