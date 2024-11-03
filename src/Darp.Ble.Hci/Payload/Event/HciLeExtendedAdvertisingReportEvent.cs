using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_LE_Extended_Advertising_Report event indicates that one or more Bluetooth devices have responded to an active scan or have broadcast advertisements that were received during a passive scan </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-e88970dc-edc8-ca27-58d8-153b97751686"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeExtendedAdvertisingReportEvent : IHciLeMetaEvent<HciLeExtendedAdvertisingReportEvent>
{
    /// <inheritdoc />
    public static HciLeMetaSubEventType SubEventType => HciLeMetaSubEventType.HCI_LE_Extended_Advertising_Report;

    /// <inheritdoc />
    public required HciLeMetaSubEventType SubEventCode { get; init; }

    /// <summary> Number of separate reports in the event </summary>
    public required byte NumReports { get; init; }
    /// <summary> The reports </summary>
    public required IReadOnlyList<HciLeExtendedAdvertisingReport> Reports { get; init; }

    /// <inheritdoc />
    public static bool TryDecode(in ReadOnlyMemory<byte> source,
        out HciLeExtendedAdvertisingReportEvent hciEvent,
        out int bytesDecoded)
    {
        bytesDecoded = default;
        hciEvent = default;
        ReadOnlySpan<byte> span = source.Span;
        byte subEventCode = span[0];
        byte numReports = span[1];
        var reports = new HciLeExtendedAdvertisingReport[numReports];
        bytesDecoded = 2;
        for (var i = 0; i < numReports; i++)
        {
            if (!HciLeExtendedAdvertisingReport.TryDecode(source[bytesDecoded..],
                    out HciLeExtendedAdvertisingReport data,
                    out int dataBytesRead))
                return false;
            bytesDecoded += dataBytesRead;
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