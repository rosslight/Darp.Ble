using System.Collections;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Darp.Ble.Data.AssignedNumbers;
using AdvertisingTypeWithData = (
    Darp.Ble.Data.AssignedNumbers.AdTypes Type,
    System.ReadOnlyMemory<byte> Bytes
);

namespace Darp.Ble.Gap;

/// <summary> The advertising data sections </summary>
public sealed class AdvertisingData : IReadOnlyList<AdvertisingTypeWithData>
{
    private readonly AdvertisingTypeWithData[] _dataSections;
    private readonly ReadOnlyMemory<byte> _advertisingDataMemory;

    private AdvertisingData(
        ReadOnlyMemory<byte> advertisingDataMemory,
        (AdTypes, ReadOnlyMemory<byte>)[] dataSections
    )
    {
        _advertisingDataMemory = advertisingDataMemory;
        _dataSections = dataSections;
    }

    /// <summary> Returns an empty advertising data object </summary>
    public static AdvertisingData Empty { get; } = new(Array.Empty<byte>(), []);

    /// <summary> Create advertising data from a given list of sections </summary>
    /// <param name="sections"> The sections to be used </param>
    /// <returns> The advertising data </returns>
    [Pure]
    public static AdvertisingData From(IReadOnlyList<(AdTypes Section, byte[] Bytes)> sections)
    {
        // Use the list wrapper to allow the conversion from byte[] to ReadOnlyMemory<byte>
        // We need this overload for cases when this conversion cannot be done automatically, e.g. when using collection literals
        return From(new ListWrapper(sections));
    }

    /// <summary> Create advertising data from a given list of sections </summary>
    /// <param name="sections"> The sections to be used </param>
    /// <returns> The advertising data </returns>
    [OverloadResolutionPriority(1)]
    [Pure]
    public static AdvertisingData From(
        IReadOnlyList<(AdTypes Section, ReadOnlyMemory<byte> Bytes)> sections
    )
    {
        ArgumentNullException.ThrowIfNull(sections);

        // Create a new byte array to hold the byte representation of the advertising data
        int bytesLength = sections.Sum(x => 2 + x.Bytes.Length);
        var advertisingDataBuffer = new byte[bytesLength];
        Span<byte> bufferSpan = advertisingDataBuffer;
        ReadOnlyMemory<byte> bufferMemory = advertisingDataBuffer;
        // Create a new array to hold the section <-> bytes representation of the advertising data
        var sectionsWithMemory = new (AdTypes Section, ReadOnlyMemory<byte> Bytes)[sections.Count];

        // Copy and map all sections
        for (var index = 0; index < sections.Count; index++)
        {
            (AdTypes section, ReadOnlyMemory<byte> originalSectionsMemory) = sections[index];
            int sectionDataLength = originalSectionsMemory.Length;
            var sectionLength = (byte)(1 + sectionDataLength);
            // Set length, type and data
            bufferSpan[0] = sectionLength;
            bufferSpan[1] = (byte)section;
            originalSectionsMemory.Span.CopyTo(bufferSpan[2..]);
            sectionsWithMemory[index] = (section, bufferMemory.Slice(2, sectionDataLength));
            // Advance views to the next section
            bufferSpan = bufferSpan[(2 + sectionDataLength)..];
            bufferMemory = bufferMemory[(2 + sectionDataLength)..];
        }

        return new AdvertisingData(advertisingDataBuffer, sectionsWithMemory);
    }

    /// <summary> Decode data sections </summary>
    /// <param name="advertisingData"> The raw bytes representing the advertising data </param>
    /// <remarks> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part C, 11 ADVERTISING AND SCAN RESPONSE DATA FORMAT </remarks>
    /// <returns> The advertisement data sections </returns>
    [Pure]
    public static AdvertisingData From(ReadOnlyMemory<byte> advertisingData)
    {
        // Copy the memory to make sure the resulting AdvertisingData is immutable
        return FromUnsafe(advertisingData.ToArray());
    }

    private static AdvertisingData FromUnsafe(ReadOnlyMemory<byte> advertisingData)
    {
        if (advertisingData.Length is 0)
            return Empty;
        // Create a new byte array to hold the byte representation of the advertising data
        ReadOnlySpan<byte> advertisingDataSpan = advertisingData.Span;
        var advertisementReports = new List<(AdTypes, ReadOnlyMemory<byte>)>();

        byte index = 0;
        // Iterate over all sections and return early if a section is invalid
        while (index < advertisingDataSpan.Length)
        {
            byte fieldLength = advertisingDataSpan[index];
            if (fieldLength == 0)
                break;
            if (index + fieldLength > advertisingDataSpan.Length)
                break;
            var fieldType = (AdTypes)advertisingDataSpan[index + 1];

            ReadOnlyMemory<byte> sectionMemory = advertisingData[
                (index + 2)..(index + 2 + fieldLength - 1)
            ];
            advertisementReports.Add((fieldType, sectionMemory));

            index += (byte)(fieldLength + 1);
        }

        return new AdvertisingData(advertisingData, advertisementReports.ToArray());
    }

    /// <summary> Creates a new advertising data object with the new section </summary>
    /// <param name="sectionType"> The Advertising data type to be added </param>
    /// <param name="sectionBytes"> The content bytes of the section </param>
    /// <returns> The newly created advertising data </returns>
    /// <remarks> In case the section already exists, it will be replaced. If not, the new section will be added in the end </remarks>
    [Pure]
    public AdvertisingData With(AdTypes sectionType, ReadOnlyMemory<byte> sectionBytes)
    {
        AdvertisingTypeWithData[] newDataSections;
        for (var i = 0; i < _dataSections.Length; i++)
        {
            if (_dataSections[i].Type != sectionType)
                continue;
            newDataSections = new AdvertisingTypeWithData[_dataSections.Length];
            _dataSections.CopyTo(newDataSections.AsSpan());
            newDataSections[i] = (sectionType, sectionBytes);
            return From(newDataSections);
        }
        newDataSections = new AdvertisingTypeWithData[_dataSections.Length + 1];
        _dataSections.CopyTo(newDataSections.AsSpan());
        newDataSections[^1] = (sectionType, sectionBytes);
        return From(newDataSections);
    }

    /// <summary> Gets the underlying data as memory </summary>
    /// <returns> The data section memory </returns>
    public ReadOnlyMemory<byte> AsReadOnlyMemory() => _advertisingDataMemory;

    /// <summary> Gives back the underlying data as byte array </summary>
    /// <returns> The data sections as byte array </returns>
    public byte[] ToByteArray() => _advertisingDataMemory.ToArray();

    /// <inheritdoc />
    public AdvertisingTypeWithData this[int index] => _dataSections[index];

    /// <inheritdoc />
    public IEnumerator<AdvertisingTypeWithData> GetEnumerator() =>
        _dataSections.AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary> Checks for a specific advertisement data type </summary>
    /// <param name="type"></param>
    /// <returns> True, if there is a section with the specified type present </returns>
    [Pure]
    public bool Contains(AdTypes type)
    {
        foreach ((AdTypes types, ReadOnlyMemory<byte> _) in _dataSections)
        {
            if (types == type)
                return true;
        }
        return false;
    }

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count" />
    public int Count => _dataSections.Length;
}

file sealed class ListWrapper(IReadOnlyList<(AdTypes Type, byte[] Bytes)> list)
    : IReadOnlyList<AdvertisingTypeWithData>
{
    private readonly IReadOnlyList<(AdTypes Type, byte[] Bytes)> _list = list;

    public IEnumerator<AdvertisingTypeWithData> GetEnumerator() =>
        _list.Select<(AdTypes, byte[]), AdvertisingTypeWithData>(x => x).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _list.Count;
    public AdvertisingTypeWithData this[int index] => _list[index];
}
