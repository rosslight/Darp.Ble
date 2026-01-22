using System.Security.Cryptography;
using Darp.Ble.Hci.Payload.Command;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Command;

public sealed class HciLeSetExtendedAdvertisingEnableCommandTests
{
    [Fact]
    public void ExpectedOpCode_ShouldBeValid()
    {
        HciLeSetExtendedAdvertisingEnableCommand.OpCode.ShouldHaveValue(0x0039 | (8 << 10));
    }

    [Theory]
    [InlineData(1, 1, new byte[] { 0 }, new ushort[] { 0 }, new byte[] { 0 }, "010100000000")]
    public void TryWriteLittleEndian_ShouldBeValid(
        byte enable,
        byte numSets,
        byte[] advertisingHandle,
        ushort[] durations,
        byte[] maxExtendedAdvertisingEvents,
        string expectedHexBytes
    )
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(6);
        var value = new HciLeSetExtendedAdvertisingEnableCommand
        {
            Enable = enable,
            NumSets = numSets,
            AdvertisingHandle = advertisingHandle,
            Duration = durations,
            MaxExtendedAdvertisingEvents = maxExtendedAdvertisingEvents,
        };

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeTrue();
        value.GetByteCount().ShouldBe(6);
        value.Enable.ShouldBe(enable);
        value.NumSets.ShouldBe(numSets);
        value.AdvertisingHandle.ToArray().ShouldBe(advertisingHandle);
        value.Duration.ToArray().ShouldBe(durations);
        value.MaxExtendedAdvertisingEvents.ToArray().ShouldBe(maxExtendedAdvertisingEvents);
        Convert.ToHexString(buffer).ShouldBe(expectedHexBytes);
    }

    [Fact]
    public void TryWriteLittleEndian_ShouldBeInvalid()
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(1);
        HciLeSetExtendedAdvertisingEnableCommand value = default;

        bool success = value.TryWriteLittleEndian(buffer);
        success.ShouldBeFalse();
    }
}
