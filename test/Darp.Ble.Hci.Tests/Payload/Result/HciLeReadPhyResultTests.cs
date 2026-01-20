using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Event;

public sealed class HciLeReadPhyResultTests
{
    [Theory]
    [InlineData("0000000101", HciCommandStatus.Success, 0x000, 1, 1)]
    public void TryReadLittleEndian_HciLeReadPhyResult_ShouldBeValid(
        string hexBytes,
        HciCommandStatus expectedStatus,
        ushort expectedConnectionHandle,
        byte expectedTxPhy,
        byte expectedRxPhy
    )
    {
        byte[] bytes = Convert.FromHexString(hexBytes);

        bool success = HciLeReadPhyResult.TryReadLittleEndian(bytes, out HciLeReadPhyResult value, out int decoded);

        success.ShouldBeTrue();
        decoded.ShouldBe(5);

        value.Status.ShouldBe(expectedStatus);
        value.ConnectionHandle.ShouldBe(expectedConnectionHandle);
        value.TxPhy.ShouldBe(expectedTxPhy);
        value.RxPhy.ShouldBe(expectedRxPhy);
    }

    [Theory]
    [InlineData("", 0)] // too short
    [InlineData("00000001", 0)] // too short
    public void TryReadLittleEndian_ShouldBeInvalid(string hexBytes, int expectedBytesDecoded)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);
        bool success = HciCommandCompleteEvent<HciLeReadPhyResult>.TryReadLittleEndian(bytes, out _, out int decoded);

        success.ShouldBeFalse();
        decoded.ShouldBe(expectedBytesDecoded);
    }
}
