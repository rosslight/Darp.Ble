using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;

namespace Darp.Ble.Gap;

public static partial class AdvertisingDataExtensions
{
    /// <summary> Creates a new advertising data object with the new section </summary>
    /// <param name="advertisingData"> The advertising data to base on </param>
    /// <param name="sectionType"> The Advertising data type to be added </param>
    /// <param name="sectionBytes"> The content bytes of the section </param>
    /// <returns> The newly created advertising data </returns>
    /// <remarks> In case the section already exists, it will be replaced. If not, the new section will be added in the end </remarks>
    [Pure]
    public static AdvertisingData With(this AdvertisingData advertisingData, AdTypes sectionType, byte[] sectionBytes)
    {
        ArgumentNullException.ThrowIfNull(advertisingData);
        return advertisingData.With(sectionType, sectionBytes);
    }

    /// <summary> Create a new <see cref="AdvertisingData"/> object with the <see cref="AdTypes.Flags"/> section created or updated </summary>
    /// <param name="advertisingData"> The advertising data to base on </param>
    /// <param name="flags"> The flags to set </param>
    /// <returns> The new advertising data </returns>
    [Pure]
    public static AdvertisingData WithFlags(this AdvertisingData advertisingData, AdvertisingDataFlags flags)
    {
        ArgumentNullException.ThrowIfNull(advertisingData);
        return advertisingData.With(AdTypes.Flags, [(byte)flags]);
    }

    /// <summary> Create a new <see cref="AdvertisingData"/> object with the <see cref="AdTypes.CompleteLocalName"/> section created or updated </summary>
    /// <param name="advertisingData"> The advertising data to base on </param>
    /// <param name="completeLocalName"> The complete local name to set </param>
    /// <returns> The new advertising data </returns>
    [Pure]
    public static AdvertisingData WithCompleteLocalName(this AdvertisingData advertisingData, string completeLocalName)
    {
        ArgumentNullException.ThrowIfNull(advertisingData);
        ArgumentNullException.ThrowIfNull(completeLocalName);
        return advertisingData.With(AdTypes.CompleteLocalName, Encoding.UTF8.GetBytes(completeLocalName));
    }

    /// <summary> Create a new <see cref="AdvertisingData"/> object with the <see cref="AdTypes.ShortenedLocalName"/> section created or updated </summary>
    /// <param name="advertisingData"> The advertising data to base on </param>
    /// <param name="shortenedLocalName"> The shortened local name to set </param>
    /// <returns> The new advertising data </returns>
    [Pure]
    public static AdvertisingData WithShortenedLocalName(this AdvertisingData advertisingData,
        string shortenedLocalName)
    {
        ArgumentNullException.ThrowIfNull(advertisingData);
        ArgumentNullException.ThrowIfNull(shortenedLocalName);
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
    [Pure]
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
    [Pure]
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
    [Pure]
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
    [Pure]
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
            byte[] bytes = GetServiceUuidBytes(bleUuids, numberOf16BitUuids, BleUuidType.Uuid16);
            advertisingData = advertisingData.With(type16Bit, bytes);
        }

        if (numberOf32BitUuids > 0)
        {
            byte[] bytes = GetServiceUuidBytes(bleUuids, numberOf32BitUuids, BleUuidType.Uuid32);
            advertisingData = advertisingData.With(type32Bit, bytes);
        }

        if (numberOf128BitUuids > 0)
        {
            byte[] bytes = GetServiceUuidBytes(bleUuids, numberOf128BitUuids, BleUuidType.Uuid128);
            advertisingData = advertisingData.With(type128Bit, bytes);
        }

        return advertisingData;
    }

    private static byte[] GetServiceUuidBytes(IReadOnlyCollection<BleUuid> additionalUuids,
        int numberOfUuids,
        BleUuidType targetType)
    {
        var numberOfBytes = (int)targetType;
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
    [Pure]
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
    [Pure]
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