using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> The HCI_LE_Extended_Advertising_Report event indicates that one or more Bluetooth devices have responded to an active scan or have broadcast advertisements that were received during a passive scan </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-e88970dc-edc8-ca27-58d8-153b97751686"/>
public readonly record struct HciLeExtendedAdvertisingReportEvent : IHciLeMetaEvent<HciLeExtendedAdvertisingReportEvent>
{
    /// <inheritdoc />
    public static HciLeMetaSubEventType SubEventType => HciLeMetaSubEventType.HCI_LE_Extended_Advertising_Report;

    /// <inheritdoc />
    public required HciLeMetaSubEventType SubEventCode { get; init; }

    /// <summary> Number of separate reports in the event </summary>
    public required byte NumReports { get; init; }
    /// <summary> The reports </summary>
    [BinaryElementCount(nameof(NumReports))]
    public required IReadOnlyList<HciLeExtendedAdvertisingReport> Reports { get; init; }

    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out HciLeExtendedAdvertisingReportEvent value)
    {
        return TryReadLittleEndian(source, out value, out _);
    }

    /// <inheritdoc />
    public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, out HciLeExtendedAdvertisingReportEvent value, out int bytesRead)
    {
        bytesRead = 0;
        value = default;
        byte subEventCode = source[0];
        byte numReports = source[1];
        var reports = new HciLeExtendedAdvertisingReport[numReports];
        bytesRead = 2;
        for (var i = 0; i < numReports; i++)
        {
            if (!HciLeExtendedAdvertisingReport.TryReadLittleEndian(source[bytesRead..],
                    out HciLeExtendedAdvertisingReport data,
                    out int dataBytesRead))
            {
                return false;
            }
            bytesRead += dataBytesRead;
            reports[i] = data;
        }
        value = new HciLeExtendedAdvertisingReportEvent
        {
            SubEventCode = (HciLeMetaSubEventType)subEventCode,
            NumReports = numReports,
            Reports = reports,
        };
        return true;
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out HciLeExtendedAdvertisingReportEvent value)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public static bool TryReadBigEndian(ReadOnlySpan<byte> source, out HciLeExtendedAdvertisingReportEvent value, out int bytesRead)
    {
        throw new NotSupportedException();
    }
}