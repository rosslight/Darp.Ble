using System.Collections;
using System.Collections.ObjectModel;
using Darp.Ble.Data.AssignedNumbers;

namespace Darp.Ble.Gap;

/// <summary> The advertising data sections </summary>
public sealed class AdvertisingData : IReadOnlyList<(AdTypes Type, ReadOnlyMemory<byte> Bytes)>
{
    private readonly IReadOnlyList<(AdTypes, ReadOnlyMemory<byte>)> _dataSections;
    private readonly ReadOnlyMemory<byte> _advertisingDataMemory;

    private AdvertisingData(ReadOnlyMemory<byte> advertisingDataMemory,
        IReadOnlyList<(AdTypes, ReadOnlyMemory<byte>)> dataSections)
    {
        _advertisingDataMemory = advertisingDataMemory;
        _dataSections = dataSections;
    }

    /// <summary> Create advertising data from a given list of sections </summary>
    /// <param name="sections"> The sections to be used </param>
    /// <returns> The advertising data </returns>
    public static AdvertisingData From(IReadOnlyList<(AdTypes Section, byte[] Bytes)> sections)
    {
        int bytesLength = sections.Sum(x => 2 + x.Bytes.Length);
        var bytes = new byte[bytesLength];
        Span<byte> bytesBuffer = bytes;
        var sectionsWithMemory = new (AdTypes Section, ReadOnlyMemory<byte> Bytes)[sections.Count];
        for (var index = 0; index < sections.Count; index++)
        {
            (AdTypes section, byte[] sectionBytes) = sections[index];
            var sectionLength = (byte)(1 + sectionBytes.Length);
            bytesBuffer[0] = sectionLength;
            bytesBuffer[1] = (byte)section;
            sectionBytes.CopyTo(bytesBuffer[2..]);
            bytesBuffer = bytesBuffer[(2 + sectionBytes.Length)..];
            sectionsWithMemory[index] = (section, sectionBytes);
        }

        return new AdvertisingData(bytes, sectionsWithMemory);
    }

    /// <summary> Decode data sections </summary>
    /// <param name="advertisingDataMemory"></param>
    /// <remarks> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part C, 11 ADVERTISING AND SCAN RESPONSE DATA FORMAT </remarks>
    /// <returns> The advertisement data sections </returns>
    public static AdvertisingData From(ReadOnlyMemory<byte> advertisingDataMemory)
    {
        var advertisementReports = new List<(AdTypes, ReadOnlyMemory<byte>)>();
        byte index = 0;
        ReadOnlySpan<byte> span = advertisingDataMemory.Span;
        while (index < span.Length)
        {
            byte fieldLength = span[index];
            if (fieldLength == 0)
                break;
            if (index + fieldLength > span.Length)
                break;
            var fieldType = (AdTypes)span[index + 1];

            ReadOnlyMemory<byte> sectionMemory = advertisingDataMemory[(index + 2)..(index + 2 + fieldLength - 1)];

            advertisementReports.Add((fieldType, sectionMemory));
            index += (byte)(fieldLength + 1);
        }
        return new AdvertisingData(
            advertisingDataMemory,
            new ReadOnlyCollection<(AdTypes, ReadOnlyMemory<byte>)>(advertisementReports)
        );
    }

    /// <inheritdoc />
    public IEnumerator<(AdTypes Type, ReadOnlyMemory<byte> Bytes)> GetEnumerator() => _dataSections.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    /// <inheritdoc />
    public int Count => _dataSections.Count;
    /// <inheritdoc />
    public (AdTypes Type, ReadOnlyMemory<byte> Bytes) this[int index] => _dataSections[index];
    /// <summary> Gets the underlying data as memory </summary>
    /// <returns> The data section memory </returns>
    public ReadOnlyMemory<byte> AsReadOnlyMemory() => _advertisingDataMemory;
    /// <summary> Gives back the underlying data as byte array </summary>
    /// <returns> The data sections as byte array </returns>
    public byte[] ToByteArray() => _advertisingDataMemory.ToArray();
}