using System.Security.Cryptography;
using Darp.Ble.Hci.Payload.Command;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeWriteSuggestedDefaultDataLengthCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeWriteSuggestedDefaultDataLengthCommand.OpCode.ShouldHaveValue(0x0024 | (0x08 << 10));
    }

    [Theory]
    [InlineData(65, 328, "41004801")]
    public void TryWriteLittleEndian_ShouldBeValid(
        ushort suggestedMaxTxOctets,
        ushort suggestedMaxTxTime,
        string expectedHexBytes
    )
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(4);
        var value = new HciLeWriteSuggestedDefaultDataLengthCommand
        {
            SuggestedMaxTxOctets = suggestedMaxTxOctets,
            SuggestedMaxTxTime = suggestedMaxTxTime,
        };

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeTrue();
        value.GetByteCount().ShouldBe(4);
        value.SuggestedMaxTxOctets.ShouldBe(suggestedMaxTxOctets);
        value.SuggestedMaxTxTime.ShouldBe(suggestedMaxTxTime);
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(3);
        HciLeWriteSuggestedDefaultDataLengthCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
