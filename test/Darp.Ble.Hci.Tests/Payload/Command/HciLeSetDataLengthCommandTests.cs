using System.Security.Cryptography;
using Darp.Ble.Hci.Payload.Command;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeSetDataLengthCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeSetDataLengthCommand.OpCode.ShouldHaveValue(0x0022 | (0x08 << 10));
    }

    [Theory]
    [InlineData(0, 65, 328, "000041004801")]
    public void TryWriteLittleEndian_ShouldBeValid(
        ushort handle,
        ushort txOctets,
        ushort txTime,
        string expectedHexBytes
    )
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(6);
        var value = new HciLeSetDataLengthCommand
        {
            ConnectionHandle = handle,
            TxOctets = txOctets,
            TxTime = txTime,
        };

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeTrue();
        value.GetByteCount().ShouldBe(6);
        value.ConnectionHandle.ShouldBe(handle);
        value.TxOctets.ShouldBe(txOctets);
        value.TxTime.ShouldBe(txTime);
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        var buffer = new byte[5];
        HciLeSetDataLengthCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
