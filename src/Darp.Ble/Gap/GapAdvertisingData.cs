using System.Collections;
using System.Collections.ObjectModel;
using Darp.Ble.Data;

namespace Darp.Ble.Gap;

/// <summary> The advertising data sections </summary>
public sealed class GapAdvertisingData : IReadOnlyList<(AdType Section, ReadOnlyMemory<byte> Bytes)>
{
    private readonly IReadOnlyList<(AdType, ReadOnlyMemory<byte>)> _dataSections;
    private readonly ReadOnlyMemory<byte> _advertisingDataMemory;

    private GapAdvertisingData(ReadOnlyMemory<byte> advertisingDataMemory,
        IReadOnlyList<(AdType, ReadOnlyMemory<byte>)> dataSections)
    {
        _advertisingDataMemory = advertisingDataMemory;
        _dataSections = dataSections;
    }

    /// <summary> Create advertising data from a given list of sections </summary>
    /// <param name="sections"> The sections to be used </param>
    /// <returns> The advertising data </returns>
    public static GapAdvertisingData From(IReadOnlyList<(AdType Section, byte[] Bytes)> sections)
    {
        int bytesLength = sections.Select(x => 2 + x.Bytes.Length).Sum();
        var bytes = new byte[bytesLength];
        Span<byte> bytesBuffer = bytes;
        var sectionsWithMemory = new (AdType Section, ReadOnlyMemory<byte> Bytes)[sections.Count];
        for (var index = 0; index < sections.Count; index++)
        {
            (AdType section, byte[] sectionBytes) = sections[index];
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
        var advertisementReports = new List<(AdType, ReadOnlyMemory<byte>)>();
        byte index = 0;
        ReadOnlySpan<byte> span = advertisingDataMemory.Span;
        while (index < span.Length)
        {
            byte fieldLength = span[index];
            if (fieldLength == 0)
                break;
            if (index + fieldLength > span.Length)
                break;
            var fieldType = (AdType)span[index + 1];

            ReadOnlyMemory<byte> sectionMemory = advertisingDataMemory[(index + 2)..(index + 2 + fieldLength - 1)];

            advertisementReports.Add((fieldType, sectionMemory));
            index += (byte)(fieldLength + 1);
        }
        return new GapAdvertisingData(
            advertisingDataMemory,
            new ReadOnlyCollection<(AdType, ReadOnlyMemory<byte>)>(advertisementReports)
        );
    }

    /// <inheritdoc />
    public IEnumerator<(AdType Section, ReadOnlyMemory<byte> Bytes)> GetEnumerator() => _dataSections.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    /// <inheritdoc />
    public int Count => _dataSections.Count;
    /// <inheritdoc />
    public (AdType Section, ReadOnlyMemory<byte> Bytes) this[int index] => _dataSections[index];
    /// <summary> Gets the underlying data as memory </summary>
    /// <returns> The data section memory </returns>
    public ReadOnlyMemory<byte> AsReadOnlyMemory() => _advertisingDataMemory;
    /// <summary> Gives back the underlying data as byte array </summary>
    /// <returns> The data sections as byte array </returns>
    public byte[] ToByteArray() => _advertisingDataMemory.ToArray();
}