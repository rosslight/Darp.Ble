using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Event;

public sealed class HciLeExtendedAdvertisingReportEventTests
{
    [Fact]
    public void SubEventType_ShouldBeValid()
    {
        HciLeExtendedAdvertisingReportEvent.SubEventType.ShouldHaveValue(0x0D);
    }

    [Fact]
    public void TryReadLittleEndian_SingleReport_ShouldBeValid()
    {
        byte[] bytes = Convert.FromHexString("0D01130001AABBCCDDEEFF0100FF7FC400000000000000000003020106");

        bool success = HciLeExtendedAdvertisingReportEvent.TryReadLittleEndian(
            bytes,
            out HciLeExtendedAdvertisingReportEvent value,
            out int decoded
        );

        success.ShouldBeTrue();
        decoded.ShouldBe(bytes.Length);
        value.SubEventCode.ShouldBe(HciLeMetaSubEventType.HCI_LE_Extended_Advertising_Report);
        HciLeExtendedAdvertisingReport report = value.Reports.ShouldHaveSingleItem();
        report.EventType.ShouldBe((ushort)0x0013);
        report.Address.ShouldBe((UInt48)0xFFEEDDCCBBAA);
        report.Rssi.ShouldBe((sbyte)-60);
        report.Data.ToArray().ShouldBe(Convert.FromHexString("020106"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0D")]
    [InlineData("0D01")]
    public void TryReadLittleEndian_Truncated_ShouldBeInvalid(string hexBytes)
    {
        byte[] bytes = Convert.FromHexString(hexBytes);

        bool success = HciLeExtendedAdvertisingReportEvent.TryReadLittleEndian(bytes, out _, out int decoded);

        success.ShouldBeFalse();
        decoded.ShouldBe(0);
    }
}
