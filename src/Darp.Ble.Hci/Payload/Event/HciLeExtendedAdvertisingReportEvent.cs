using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Event;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciLeExtendedAdvertisingReportEvent : IHciLeMetaEvent<HciLeExtendedAdvertisingReportEvent>
{
    public static HciLeMetaSubEventType SubEventType => HciLeMetaSubEventType.HCI_LE_Extended_Advertising_Report;

    public required HciLeMetaSubEventType SubEventCode { get; init; }
    public required byte NumReports { get; init; }
    public required IReadOnlyList<HciLeExtendedAdvertisingReport> Reports { get; init; }

    public static bool TryDecode(in ReadOnlyMemory<byte> buffer,
        out HciLeExtendedAdvertisingReportEvent hciEvent,
        out int bytesRead)
    {
        bytesRead = default;
        hciEvent = default;
        ReadOnlySpan<byte> span = buffer.Span;
        byte subEventCode = span[0];
        byte numReports = span[1];
        var reports = new HciLeExtendedAdvertisingReport[numReports];
        bytesRead = 2;
        for (var i = 0; i < numReports; i++)
        {
            if (!HciLeExtendedAdvertisingReport.TryDecode(buffer[bytesRead..],
                    out HciLeExtendedAdvertisingReport data,
                    out int dataBytesRead))
                return false;
            bytesRead += dataBytesRead;
            reports[i] = data;
        }
        hciEvent = new HciLeExtendedAdvertisingReportEvent
        {
            SubEventCode = (HciLeMetaSubEventType)subEventCode,
            NumReports = numReports,
            Reports = reports,
        };
        return true;
    }
}