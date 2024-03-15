using System.Collections;
using System.Collections.ObjectModel;
using Darp.Ble.Data;

namespace Darp.Ble.Gap;

public sealed class GapAdvertisingData : IReadOnlyList<(SectionType Section, ReadOnlyMemory<byte> Bytes)>
{
    private readonly IReadOnlyList<(SectionType, ReadOnlyMemory<byte>)> _dataSections;
    private readonly ReadOnlyMemory<byte> _advertisingDataMemory;

    private GapAdvertisingData(ReadOnlyMemory<byte> advertisingDataMemory,
        IReadOnlyList<(SectionType, ReadOnlyMemory<byte>)> dataSections)
    {
        _advertisingDataMemory = advertisingDataMemory;
        _dataSections = dataSections;
    }

    public static GapAdvertisingData From(IReadOnlyList<(SectionType Section, byte[] Bytes)> sections)
    {
        int bytesLength = sections.Select(x => 2 + x.Bytes.Length).Sum();
        var bytes = new byte[bytesLength];
        Span<byte> bytesBuffer = bytes;
        var sectionsWithMemory = new (SectionType Section, ReadOnlyMemory<byte> Bytes)[sections.Count];
        for (var index = 0; index < sections.Count; index++)
        {
            (SectionType section, byte[] sectionBytes) = sections[index];
            var sectionLength = (byte)(1 + sectionBytes.Length);
            bytesBuffer[0] = sectionLength;
            bytesBuffer[1] = (byte)section;
            sectionBytes.CopyTo(bytesBuffer[2..]);
            bytesBuffer = bytesBuffer[(2 + sectionBytes.Length)..];
            sectionsWithMemory[index] = (section, sectionBytes);
        }

        return new GapAdvertisingData(bytes, sectionsWithMemory);
    }

    /// <summary> Decode data sections </summary>
    /// <param name="advertisingDataMemory"></param>
    /// <remarks> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part C, 11 ADVERTISING AND SCAN RESPONSE DATA FORMAT </remarks>
    /// <returns> The advertisement data sections </returns>
    public static GapAdvertisingData From(ReadOnlyMemory<byte> advertisingDataMemory)
    {
        var advertisementReports = new List<(SectionType, ReadOnlyMemory<byte>)>();
        byte index = 0;
        ReadOnlySpan<byte> span = advertisingDataMemory.Span;
        while (index < span.Length)
        {
            byte fieldLength = span[index];
            if (fieldLength == 0)
                break;
            if (index + fieldLength > span.Length)
                break;
            var fieldType = (SectionType)span[index + 1];

            ReadOnlyMemory<byte> sectionMemory = advertisingDataMemory[(index + 2)..(index + 2 + fieldLength - 1)];

            advertisementReports.Add((fieldType, sectionMemory));
            index += (byte)(fieldLength + 1);
        }
        return new GapAdvertisingData(
            advertisingDataMemory,
            new ReadOnlyCollection<(SectionType, ReadOnlyMemory<byte>)>(advertisementReports)
        );
    }

    /// <inheritdoc />
    public IEnumerator<(SectionType Section, ReadOnlyMemory<byte> Bytes)> GetEnumerator() => _dataSections.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    /// <inheritdoc />
    public int Count => _dataSections.Count;
    /// <inheritdoc />
    public (SectionType Section, ReadOnlyMemory<byte> Bytes) this[int index] => _dataSections[index];

    public ReadOnlyMemory<byte> AsReadOnlyMemory() => _advertisingDataMemory;
    public byte[] ToByteArray() => _advertisingDataMemory.ToArray();
}