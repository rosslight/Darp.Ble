using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Gatt.Att;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt.Database;

/// <summary> The database </summary>
#pragma warning disable CA1710 // Identifiers should have correct suffix -> Collection is not the primary usecase
public interface IGattDatabase : IReadOnlyCollection<GattDatabaseEntry>
#pragma warning restore CA1710
{
    /// <summary> The handle of the first attribute in the database </summary>
    ushort MinHandle { get; }

    /// <summary> The handle of the last attribute in the database </summary>
    ushort MaxHandle { get; }

    /// <summary> Tries to get the handle of a given attribute </summary>
    /// <param name="attribute"> The attribute to look for </param>
    /// <param name="handle"> The attribute handle </param>
    /// <returns> True, if a handle could be found; False, otherwise </returns>
    bool TryGetHandle(IGattAttribute attribute, out ushort handle);

    /// <summary> Tries to get the attribute of a given handle </summary>
    /// <param name="handle"> The handle to get the attribute for </param>
    /// <param name="attribute"> The attribute </param>
    /// <returns> True, if an attribute could be found; False, otherwise </returns>
    bool TryGetAttribute(ushort handle, [NotNullWhen(true)] out IGattAttribute? attribute);

    /// <summary> Get the handle of a given attribute </summary>
    /// <param name="attribute"> The attribute to look for </param>
    /// <returns> The attribute handle </returns>
    /// <exception cref="KeyNotFoundException"> Thrown, if the attribute was not present in the database </exception>
    ushort this[IGattAttribute attribute] { get; }

    /// <summary> Get all service group entries starting from a specific handle </summary>
    /// <param name="startHandle"> The handle to start searching from </param>
    /// <returns> An enumerable containing all service group entries </returns>
    IEnumerable<GattDatabaseGroupEntry> GetServiceEntries(ushort startHandle);

    /// <summary> Add a service at the end of the database </summary>
    /// <param name="service"> The service to be added </param>
    internal void AddService(IGattClientService service);

    /// <summary> Add a characteristic and the characteristic value at the end of the service section </summary>
    /// <param name="characteristic"> The characteristic to be added </param>
    /// <exception cref="KeyNotFoundException"> Thrown when the service the characteristic is contained in is unknown </exception>
    internal void AddCharacteristic(IGattClientCharacteristic characteristic);

    /// <summary> Add a descriptor at the end of the characteristic section </summary>
    /// <param name="characteristic"> The characteristic the descriptor is associated to </param>
    /// <param name="descriptor"> The descriptor to be added </param>
    /// <exception cref="KeyNotFoundException"> Thrown when the characteristic the descriptor is contained in is unknown </exception>
    internal void AddDescriptor(IGattClientCharacteristic characteristic, IGattCharacteristicValue descriptor);

    /// <summary> Hash the gatt database </summary>
    /// <returns> The has as UInt128 </returns>
    UInt128 CreateHash();
}
