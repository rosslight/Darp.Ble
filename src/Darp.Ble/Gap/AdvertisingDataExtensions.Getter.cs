using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;

namespace Darp.Ble.Gap;

/// <summary> Extensions for advertising data </summary>
public static partial class AdvertisingDataExtensions
{
    /// <summary> Get the AD Flags if it's contained in the given data. </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <param name="flags"> The resulting flags if the return is true </param>
    /// <returns> True, if the data type and bytes were present </returns>
    public static bool TryGetFlags(this AdvertisingData data, out AdvertisingDataFlags flags)
    {
        ArgumentNullException.ThrowIfNull(data);
        flags = default;
        var adTypeFound = false;
        foreach ((AdTypes adTypes, ReadOnlyMemory<byte> bytes) in data)
        {
            if (adTypes is not AdTypes.Flags || bytes.Length == 0)
                continue;
            adTypeFound = true;
            flags |= (AdvertisingDataFlags)bytes.Span[0];
        }
        return adTypeFound;
    }

    /// <summary> Get the first entry of a given advertising data type </summary>
    /// <param name="data"> The advertising data </param>
    /// <param name="type"> The advertising data type </param>
    /// <param name="buffer"> The bytes representing the section </param>
    /// <returns> True, if the type was found </returns>
    public static bool TryGetFirstType(this AdvertisingData data, AdTypes type, out ReadOnlyMemory<byte> buffer)
    {
        ArgumentNullException.ThrowIfNull(data);
        foreach ((AdTypes adTypes, ReadOnlyMemory<byte> bytes) in data)
        {
            if (adTypes != type)
                continue;
            buffer = bytes;
            return true;
        }
        buffer = ReadOnlyMemory<byte>.Empty;
        return false;
    }

    /// <summary>
    /// Get the AD Complete Local Name if its contained in the given data.
    /// Will return only first name if specified multiple times
    /// </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <param name="name"> The resulting name if the return is true </param>
    /// <returns> True, if the data type was present </returns>
    public static bool TryGetCompleteLocalName(this AdvertisingData data, [NotNullWhen(true)] out string? name)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.TryGetFirstType(AdTypes.CompleteLocalName, out ReadOnlyMemory<byte> buffer))
        {
            name = Encoding.UTF8.GetString(buffer.Span);
            return true;
        }
        name = null;
        return false;
    }

    /// <summary>
    /// Get the AD Shortened Local Name if its contained in the given data.
    /// Will return only first name if specified multiple times
    /// </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <param name="name"> The resulting name if the return is true </param>
    /// <returns> True, if the data type was present </returns>
    public static bool TryGetShortenedLocalName(this AdvertisingData data, [NotNullWhen(true)] out string? name)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.TryGetFirstType(AdTypes.ShortenedLocalName, out ReadOnlyMemory<byte> buffer))
        {
            name = Encoding.UTF8.GetString(buffer.Span);
            return true;
        }
        name = null;
        return false;
    }

    /// <summary>
    /// Gets the AD Complete Local Name if possible. Looks for Shortened Local Name afterward.
    /// Will only return first name is specified multiple times
    /// </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <param name="name"> The resulting name if the return is true </param>
    /// <returns> True, if the data type was present </returns>
    public static bool TryGetLocalName(this AdvertisingData data, [NotNullWhen(true)] out string? name)
    {
        if (data.TryGetCompleteLocalName(out name))
            return true;
        if (data.TryGetShortenedLocalName(out name))
            return true;
        name = null;
        return false;
    }

    /// <summary> Accumulate any services </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <returns> An array with services </returns>
    public static IEnumerable<BleUuid> GetServiceUuids(this AdvertisingData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return data.Count is 0 ? Array.Empty<BleUuid>() : GetServicesInt(data);

        IEnumerable<BleUuid> GetServicesInt(AdvertisingData d)
        {
            foreach ((AdTypes sectionType, ReadOnlyMemory<byte> bytes) in d)
            {
                int guidLength = sectionType switch
                {
                    AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids
                    or AdTypes.CompleteListOf16BitServiceOrServiceClassUuids => 2,
                    AdTypes.IncompleteListOf32BitServiceOrServiceClassUuids
                    or AdTypes.CompleteListOf32BitServiceOrServiceClassUuids => 4,
                    AdTypes.IncompleteListOf128BitServiceOrServiceClassUuids
                    or AdTypes.CompleteListOf128BitServiceOrServiceClassUuids => 16,
                    _ => -1,
                };
                if (guidLength < 0)
                    continue;
                // Using length - 1 to avoid crashing if invalid lengths were transmitted
                for (var i = 0; i < bytes.Length + 1 - guidLength; i += guidLength)
                    yield return new BleUuid(bytes[i..(i + guidLength)].Span);
            }
        }
    }

    /// <summary>
    /// Get the AD Manufacturer Specific Data if its contained in the given advertising data.
    /// Will return only first data found
    /// </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <param name="companyUuid"> The company uuid to look for </param>
    /// <param name="manufacturerData"> The resulting manufacturer specific data if the return is true </param>
    /// <returns> True, if the data type was present and AD data at least 2 bytes long </returns>
    public static bool TryGetManufacturerSpecificData(
        this AdvertisingData data,
        CompanyIdentifiers companyUuid,
        out ReadOnlyMemory<byte> manufacturerData
    )
    {
        ArgumentNullException.ThrowIfNull(data);
        foreach ((AdTypes adTypes, ReadOnlyMemory<byte> bytes) in data)
        {
            if (adTypes != AdTypes.ManufacturerSpecificData)
                continue;
            if (bytes.Length < 2)
                continue;
            ushort uuid = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Span);
            if (uuid != (ushort)companyUuid)
            {
                continue;
            }

            manufacturerData = bytes[2..];
            return true;
        }
        manufacturerData = default;
        return false;
    }

    /// <summary>
    /// Get the AD Manufacturer Specific Data if its contained in the given advertising data.
    /// Will return only first data found
    /// </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <param name="companyUuid"> The company uuid of the first manufacturer specific data found </param>
    /// <param name="manufacturerData"> The resulting manufacturer specific data if the return is true </param>
    /// <returns> True, if the data type was present and AD data at least 2 bytes long </returns>
    public static bool TryGetManufacturerSpecificData(
        this AdvertisingData data,
        out CompanyIdentifiers companyUuid,
        out ReadOnlyMemory<byte> manufacturerData
    )
    {
        ArgumentNullException.ThrowIfNull(data);
        if (!data.TryGetFirstType(AdTypes.ManufacturerSpecificData, out ReadOnlyMemory<byte> bytes) || bytes.Length < 2)
        {
            companyUuid = default;
            manufacturerData = default;
            return false;
        }
        companyUuid = (CompanyIdentifiers)BinaryPrimitives.ReadUInt16LittleEndian(bytes.Span);
        manufacturerData = bytes[2..];
        return true;
    }
}
