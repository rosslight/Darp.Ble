using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.HciHost.Gap;

/// <summary>
/// Joins the reports of one advertisement. A controller splits data that does not fit one report: every report but
/// the last says more data is to come. Reports of other advertisers and scan responses may come in between
/// </summary>
/// <remarks> Only the transport's read loop uses it </remarks>
internal sealed class AdvertisingReportAssembler
{
    // The most data an advertisement can carry
    private const int MaxDataLength = 1650;
    private const int ScanResponse = 0b01000;
    private const int MoreDataToCome = 0b01;
    private const sbyte TxPowerNotAvailable = 0x7F;

    private readonly Dictionary<Sender, Pending> _pending = [];

    /// <summary> The data of the whole advertisement, once a report ends it </summary>
    /// <param name="report"> The next report </param>
    /// <param name="data"> The data of the advertisement this report ends </param>
    /// <param name="txPower"> Its TX power: later reports may leave out the one an earlier report gave </param>
    /// <returns> False while more data is to come, or when an advertisement is longer than any can be </returns>
    public bool TryEnd(HciLeExtendedAdvertisingReport report, out ReadOnlyMemory<byte> data, out sbyte txPower)
    {
        // Bits 5 and 6 of the event type: complete, more data to come, or truncated
        bool moreToCome = ((report.EventType >> 5) & 0b11) == MoreDataToCome;
        var sender = new Sender(
            report.AddressType,
            report.Address.ToUInt64(),
            report.AdvertisingSId,
            (report.EventType & ScanResponse) != 0
        );
        bool continues = _pending.Remove(sender, out Pending earlier);
        txPower = continues && report.TxPower == TxPowerNotAvailable ? earlier.TxPower : report.TxPower;
        data = default;
        if (!continues && !moreToCome)
        {
            data = report.Data;
            return true;
        }
        // An advertisement already too long is dropped up to its last report
        byte[]? joined = continues && earlier.Data is null ? null : [.. earlier.Data ?? [], .. report.Data.Span];
        if (joined?.Length > MaxDataLength)
            joined = null;
        if (moreToCome)
        {
            _pending[sender] = new Pending(joined, txPower);
            return false;
        }
        if (joined is null)
            return false;
        data = joined;
        return true;
    }

    /// <summary> A new scan starts without the parts of the last one </summary>
    public void Clear() => _pending.Clear();

    private readonly record struct Sender(byte AddressType, ulong Address, byte AdvertisingSId, bool IsScanResponse);

    /// <summary> The data so far, or null for an advertisement too long to deliver </summary>
    private readonly record struct Pending(byte[]? Data, sbyte TxPower);
}
