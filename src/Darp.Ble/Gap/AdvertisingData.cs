using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Implementation;
using AdvertisingTypeWithData = (Darp.Ble.Data.AssignedNumbers.AdTypes Type, System.ReadOnlyMemory<byte> Bytes);

namespace Darp.Ble.Gap;

/// <summary> The advertising data sections </summary>
public sealed class AdvertisingData : IReadOnlyList<AdvertisingTypeWithData>
{
    private readonly AdvertisingTypeWithData[] _dataSections;
    private readonly ReadOnlyMemory<byte> _advertisingDataMemory;

    private AdvertisingData(ReadOnlyMemory<byte> advertisingDataMemory,
        (AdTypes, ReadOnlyMemory<byte>)[] dataSections)
    {
        _advertisingDataMemory = advertisingDataMemory;
        _dataSections = dataSections;
    }

    /// <summary> Returns an empty advertising data object </summary>
    public static AdvertisingData Empty { get; } = new(Array.Empty<byte>(), []);

    /// <summary> Create advertising data from a given list of sections </summary>
    /// <param name="sections"> The sections to be used </param>
    /// <returns> The advertising data </returns>
    public static AdvertisingData From(IReadOnlyList<(AdTypes Section, byte[] Bytes)> sections)
    {
        return From(sections.Select(x => (x.Section, (ReadOnlyMemory<byte>)x.Bytes)).ToArray());
    }

    /// <summary> Create advertising data from a given list of sections </summary>
    /// <param name="sections"> The sections to be used </param>
    /// <returns> The advertising data </returns>
    [OverloadResolutionPriority(1)]
    public static AdvertisingData From(IReadOnlyList<(AdTypes Section, ReadOnlyMemory<byte> Bytes)> sections)
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

            ReadOnlyMemory<byte> sectionMemory = advertisingData[(index + 2)..(index + 2 + fieldLength - 1)];
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
    public IEnumerator<AdvertisingTypeWithData> GetEnumerator() => _dataSections.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary> Checks for a specific advertisement data type </summary>
    /// <param name="type"></param>
    /// <returns> True, if there is a section with the specified type present </returns>
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

public static partial class AdvertisingDataExtensions
{
    /// <summary> Create a new <see cref="AdvertisingData"/> object with the <see cref="AdTypes.Flags"/> section created or updated </summary>
    /// <param name="advertisingData"> The advertising data to base on </param>
    /// <param name="flags"> The flags to set </param>
    /// <returns> The new advertising data </returns>
    public static AdvertisingData WithFlags(this AdvertisingData advertisingData, AdvertisingDataFlags flags)
    {
        ArgumentNullException.ThrowIfNull(advertisingData);
        return advertisingData.With(AdTypes.Flags, (byte[]) [(byte)flags]);
    }

    /// <summary> Create a new <see cref="AdvertisingData"/> object with the <see cref="AdTypes.CompleteLocalName"/> section created or updated </summary>
    /// <param name="advertisingData"> The advertising data to base on </param>
    /// <param name="completeLocalName"> The complete local name to set </param>
    /// <returns> The new advertising data </returns>
    public static AdvertisingData WithCompleteLocalName(this AdvertisingData advertisingData, string completeLocalName)
    {
        ArgumentNullException.ThrowIfNull(advertisingData);
        return advertisingData.With(AdTypes.CompleteLocalName, Encoding.UTF8.GetBytes(completeLocalName));
    }

    /// <summary> Create a new <see cref="AdvertisingData"/> object with the <see cref="AdTypes.ShortenedLocalName"/> section created or updated </summary>
    /// <param name="advertisingData"> The advertising data to base on </param>
    /// <param name="shortenedLocalName"> The shortened local name to set </param>
    /// <returns> The new advertising data </returns>
    public static AdvertisingData WithShortenedLocalName(this AdvertisingData advertisingData, string shortenedLocalName)
    {
        ArgumentNullException.ThrowIfNull(advertisingData);
        return advertisingData.With(AdTypes.ShortenedLocalName, Encoding.UTF8.GetBytes(shortenedLocalName));
    }

    /// <summary>
    /// Create a new <see cref="AdvertisingData"/> object with the
    /// <see cref="AdTypes.CompleteListOf16BitServiceOrServiceClassUuids"/>, <see cref="AdTypes.CompleteListOf32BitServiceOrServiceClassUuids"/> or <see cref="AdTypes.CompleteListOf128BitServiceOrServiceClassUuids"/>
    /// sections created or updated </summary>
    /// <param name="advertisingData"> The advertising data to base on </param>
    /// <param name="bleUuid"> The uuid to add </param>
    /// <param name="bleUuids"> Additional uuids to add </param>
    /// <returns> The new advertising data </returns>
    public static AdvertisingData WithCompleteListOfServiceUuids(
        this AdvertisingData advertisingData,
        BleUuid bleUuid,
        params IReadOnlyCollection<BleUuid> bleUuids
    )
    {
        return advertisingData.WithCompleteListOfServiceUuids([bleUuid, ..bleUuids]);
    }

    /// <summary>
    /// Create a new <see cref="AdvertisingData"/> object with the
    /// <see cref="AdTypes.CompleteListOf16BitServiceOrServiceClassUuids"/>, <see cref="AdTypes.CompleteListOf32BitServiceOrServiceClassUuids"/> or <see cref="AdTypes.CompleteListOf128BitServiceOrServiceClassUuids"/>
    /// sections created or updated </summary>
    /// <param name="advertisingData"> The advertising data to base on </param>
    /// <param name="bleUuids"> Uuids to add </param>
    /// <returns> The new advertising data </returns>
    public static AdvertisingData WithCompleteListOfServiceUuids(
        this AdvertisingData advertisingData,
        IReadOnlyCollection<BleUuid> bleUuids
    )
    {
        return advertisingData.WithListOfServiceUuids(bleUuids,
            AdTypes.CompleteListOf16BitServiceOrServiceClassUuids,
            AdTypes.CompleteListOf32BitServiceOrServiceClassUuids,
            AdTypes.CompleteListOf128BitServiceOrServiceClassUuids);
    }

    /// <summary>
    /// Create a new <see cref="AdvertisingData"/> object with the
    /// <see cref="AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids"/>, <see cref="AdTypes.IncompleteListOf32BitServiceOrServiceClassUuids"/> or <see cref="AdTypes.IncompleteListOf128BitServiceOrServiceClassUuids"/>
    /// sections created or updated </summary>
    /// <param name="advertisingData"> The advertising data to base on </param>
    /// <param name="bleUuid"> The uuid to add </param>
    /// <param name="bleUuids"> Uuids to add </param>
    /// <returns> The new advertising data </returns>
    public static AdvertisingData WithIncompleteListOfServiceUuids(
        this AdvertisingData advertisingData,
        BleUuid bleUuid,
        params IReadOnlyCollection<BleUuid> bleUuids
    )
    {
        return advertisingData.WithIncompleteListOfServiceUuids([bleUuid, ..bleUuids]);
    }

    /// <summary>
    /// Create a new <see cref="AdvertisingData"/> object with the
    /// <see cref="AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids"/>, <see cref="AdTypes.IncompleteListOf32BitServiceOrServiceClassUuids"/> or <see cref="AdTypes.IncompleteListOf128BitServiceOrServiceClassUuids"/>
    /// sections created or updated </summary>
    /// <param name="advertisingData"> The advertising data to base on </param>
    /// <param name="bleUuids"> Additional uuids to add </param>
    /// <returns> The new advertising data </returns>
    public static AdvertisingData WithIncompleteListOfServiceUuids(
        this AdvertisingData advertisingData,
        IReadOnlyCollection<BleUuid> bleUuids
    )
    {
        return advertisingData.WithListOfServiceUuids(bleUuids,
            AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids,
            AdTypes.IncompleteListOf32BitServiceOrServiceClassUuids,
            AdTypes.IncompleteListOf128BitServiceOrServiceClassUuids);
    }

    private static AdvertisingData WithListOfServiceUuids(
        this AdvertisingData advertisingData,
        IReadOnlyCollection<BleUuid> bleUuids,
        AdTypes type16Bit,
        AdTypes type32Bit,
        AdTypes type128Bit
    )
    {
        ArgumentNullException.ThrowIfNull(advertisingData);
        ArgumentNullException.ThrowIfNull(bleUuids);
        var numberOf16BitUuids = 0;
        var numberOf32BitUuids = 0;
        var numberOf128BitUuids = 0;
        foreach (BleUuid bleUuid in bleUuids)
        {
            switch (bleUuid.Type)
            {
                case BleUuidType.Uuid16:
                    numberOf16BitUuids++;
                    break;
                case BleUuidType.Uuid32:
                    numberOf32BitUuids++;
                    break;
                case BleUuidType.Uuid128:
                    numberOf128BitUuids++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bleUuids), "Uuid of invalid type. Has to be 16, 32 or 128 bit");
            }
        }

        if (numberOf16BitUuids > 0)
        {
            byte[] bytes = GetServiceUuidBytes(bleUuids, numberOf16BitUuids, BleUuidType.Uuid16, 2);
            advertisingData = advertisingData.With(type16Bit, bytes);
        }
        if (numberOf32BitUuids > 0)
        {
            byte[] bytes = GetServiceUuidBytes(bleUuids, numberOf32BitUuids, BleUuidType.Uuid32, 4);
            advertisingData = advertisingData.With(type32Bit, bytes);
        }
        if (numberOf128BitUuids > 0)
        {
            byte[] bytes = GetServiceUuidBytes(bleUuids, numberOf128BitUuids, BleUuidType.Uuid128, 16);
            advertisingData = advertisingData.With(type128Bit, bytes);
        }
        return advertisingData;
    }

    private static byte[] GetServiceUuidBytes(IReadOnlyCollection<BleUuid> additionalUuids,
        int numberOfUuids,
        BleUuidType targetType,
        int numberOfBytes)
    {
        var bytes = new byte[numberOfUuids * numberOfBytes];
        Span<byte> byteSpan = bytes;
        foreach (BleUuid additionalUuid in additionalUuids)
        {
            if (additionalUuid.Type != targetType) continue;
            additionalUuid.TryWriteBytes(byteSpan[..numberOfBytes]);
            byteSpan = byteSpan[numberOfBytes..];
        }
        Debug.Assert(byteSpan.Length == 0, "Byte span should be of length 0");
        return bytes;
    }

    /// <summary>
    /// Create a new <see cref="AdvertisingData"/> object with the <see cref="AdTypes.ManufacturerSpecificData"/> section created or updated </summary>
    /// <param name="advertisingData"> The advertising data to base on </param>
    /// <param name="companyIdentifier"> The company identifier of the manufacturer specific data </param>
    /// <param name="manufacturerSpecificData"> The manufacturer specific data </param>
    /// <remarks> This method only exists for cases when there a byte[] is needed which cannot be converted to a ReadOnlyMemory </remarks>
    /// <returns> The new advertising data </returns>
    public static AdvertisingData WithManufacturerSpecificData(
        this AdvertisingData advertisingData,
        CompanyIdentifiers companyIdentifier,
        byte[] manufacturerSpecificData)
    {
        return advertisingData.WithManufacturerSpecificData(companyIdentifier, manufacturerSpecificData);
    }

    /// <summary>
    /// Create a new <see cref="AdvertisingData"/> object with the <see cref="AdTypes.ManufacturerSpecificData"/> section created or updated </summary>
    /// <param name="advertisingData"> The advertising data to base on </param>
    /// <param name="companyIdentifier"> The company identifier of the manufacturer specific data </param>
    /// <param name="manufacturerSpecificData"> The manufacturer specific data </param>
    /// <returns> The new advertising data </returns>
    [OverloadResolutionPriority(1)]
    public static AdvertisingData WithManufacturerSpecificData(
        this AdvertisingData advertisingData,
        CompanyIdentifiers companyIdentifier,
        ReadOnlyMemory<byte> manufacturerSpecificData)
    {
        ArgumentNullException.ThrowIfNull(advertisingData);
        Memory<byte> sectionData = new byte[manufacturerSpecificData.Length + 2];
        BinaryPrimitives.WriteUInt16LittleEndian(sectionData.Span, (ushort)companyIdentifier);
        manufacturerSpecificData.CopyTo(sectionData[2..]);
        return advertisingData.With(AdTypes.ManufacturerSpecificData, sectionData);
    }
}