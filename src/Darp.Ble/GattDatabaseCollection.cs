using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble;

/*
    Primary Service 0x1800
        Characteristic 0x2A00 (Read, Write)
            Characteristic Value (readable)
        Characteristic 0x2A01 (Read)
            Characteristic Value (readable)
    Primary Service 0x1801
        Characteristic 0x2A05 (Read, Write)
            Characteristic Value (readable)
            Descriptor 0x2902 (CCCD)
 */

public readonly struct GattDatabaseEntry(IGattAttribute attribute, ushort handle) : IGattAttribute
{
    private readonly IGattAttribute _attribute = attribute;

    /// <inheritdoc />
    public ushort Handle { get; } = handle;

    /// <inheritdoc />
    public BleUuid AttributeType => _attribute.AttributeType;

    /// <inheritdoc />
    public PermissionCheckStatus CheckReadPermissions(IGattClientPeer clientPeer) =>
        _attribute.CheckReadPermissions(clientPeer);

    /// <inheritdoc />
    public PermissionCheckStatus CheckWritePermissions(IGattClientPeer clientPeer) =>
        _attribute.CheckWritePermissions(clientPeer);

    /// <inheritdoc />
    public ValueTask<GattProtocolStatus> WriteValueAsync(IGattClientPeer? clientPeer, byte[] value) =>
        _attribute.WriteValueAsync(clientPeer, value);

    /// <inheritdoc />
    public ValueTask<byte[]> ReadValueAsync(IGattClientPeer? clientPeer) => _attribute.ReadValueAsync(clientPeer);
}

public readonly struct GattDatabaseGroupEntry(IGattAttribute attribute, ushort handle, ushort endGroupHandle)
    : IGattAttribute
{
    private readonly IGattAttribute _attribute = attribute;

    /// <inheritdoc />
    public ushort Handle { get; } = handle;

    /// <summary> The end handle of an attribute group </summary>
    public ushort EndGroupHandle { get; } = endGroupHandle;

    /// <inheritdoc />
    public BleUuid AttributeType => _attribute.AttributeType;

    /// <inheritdoc />
    public PermissionCheckStatus CheckReadPermissions(IGattClientPeer clientPeer) =>
        _attribute.CheckReadPermissions(clientPeer);

    /// <inheritdoc />
    public PermissionCheckStatus CheckWritePermissions(IGattClientPeer clientPeer) =>
        _attribute.CheckWritePermissions(clientPeer);

    /// <inheritdoc />
    public ValueTask<GattProtocolStatus> WriteValueAsync(IGattClientPeer? clientPeer, byte[] value) =>
        _attribute.WriteValueAsync(clientPeer, value);

    /// <inheritdoc />
    public ValueTask<byte[]> ReadValueAsync(IGattClientPeer? clientPeer) => _attribute.ReadValueAsync(clientPeer);
}

/// <summary> The gatt database </summary>
public sealed class GattDatabaseCollection : IReadOnlyCollection<GattDatabaseEntry>
{
    /// <summary> UUID for <c>Primary Service</c> </summary>
    /// <value> 0x2800 </value>
    public static readonly BleUuid PrimaryServiceType = 0x2800;

    /// <summary> UUID for <c>Secondary Service</c> </summary>
    /// <value> 0x2801 </value>
    public static readonly BleUuid SecondaryServiceType = 0x2801;

    /// <summary> UUID for <c>Characteristic</c> </summary>
    /// <value> 0x2803 </value>
    public static readonly BleUuid CharacteristicType = 0x2803;
    public static readonly BleUuid UserDescriptionType = 0x2901;

    private readonly object _lock = new();
    private readonly List<IGattAttribute> _attributes = [];

    /// <summary> The handle of the first attribute in the database </summary>
    public ushort MinHandle => 0x0001;

    /// <summary> The handle of the last attribute in the database </summary>
    public ushort MaxHandle
    {
        get
        {
            lock (_lock)
            {
                return (ushort)(_attributes.Count + 1);
            }
        }
    }

    /// <summary> Add a service at the end of the database </summary>
    /// <param name="service"> The service to be added </param>
    internal void AddService(IGattClientService service)
    {
        lock (_lock)
        {
            _attributes.Add(service.Declaration);
        }
    }

    /// <summary> Add a characteristic and the characteristic value at the end of the service section </summary>
    /// <param name="characteristic"> The characteristic to be added </param>
    /// <exception cref="KeyNotFoundException"> Thrown when the service the characteristic is contained in is unknown </exception>
    internal void AddCharacteristic(IGattClientCharacteristic characteristic)
    {
        lock (_lock)
        {
            ArgumentNullException.ThrowIfNull(characteristic);
            ushort serviceHandle = GetHandle(characteristic.Service.Declaration);
            ushort serviceEndHandle = GetServiceEndHandle(serviceHandle);
            int serviceEndIndex = serviceEndHandle - 1;
            _attributes.Insert(serviceEndIndex + 1, characteristic.Declaration);
            _attributes.Insert(serviceEndIndex + 2, characteristic.Value);
        }
    }

    /// <summary> Searches the database for the next service and returns the last handle before </summary>
    /// <param name="serviceHandle"> The service handle to start at </param>
    /// <returns> The end group handle of the service </returns>
    private ushort GetServiceEndHandle(ushort serviceHandle)
    {
        var serviceEndIndex = (ushort)(serviceHandle - 1);
        for (; serviceEndIndex < _attributes.Count - 1; serviceEndIndex++)
        {
            IGattAttribute attribute = _attributes[serviceEndIndex + 1];
            if (
                attribute.AttributeType.Equals(PrimaryServiceType)
                || attribute.AttributeType.Equals(SecondaryServiceType)
            )
            {
                // Break if the next service was found
                break;
            }
        }
        return (ushort)(serviceEndIndex + 1);
    }

    /// <summary> Add a descriptor at the end of the characteristic section </summary>
    /// <param name="descriptor"> The descriptor to be added </param>
    /// <exception cref="KeyNotFoundException"> Thrown when the characteristic the descriptor is contained in is unknown </exception>
    internal void AddDescriptor(IGattClientDescriptor descriptor)
    {
        lock (_lock)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            ushort characteristicHandle = GetHandle(descriptor.Characteristic.Declaration);
            ushort characteristicEndHandle = GetCharacteristicEndHandle(characteristicHandle);
            int characteristicEndIndex = characteristicEndHandle - 1;
            _attributes.Insert(characteristicEndIndex + 1, descriptor.Value);
        }
    }

    /// <summary> Searches the database for the next characteristic and returns the last handle before </summary>
    /// <param name="characteristicHandle"> The characteristic handle to start at </param>
    /// <returns> The end group handle of the characteristic </returns>
    private ushort GetCharacteristicEndHandle(ushort characteristicHandle)
    {
        // Start with offset of 1 to account for the characteristic value
        var characteristicEndIndex = (ushort)(characteristicHandle - 1 + 1);
        for (; characteristicEndIndex < _attributes.Count - 1; characteristicEndIndex++)
        {
            IGattAttribute attribute = _attributes[characteristicEndIndex + 1];
            if (attribute.AttributeType.Equals(CharacteristicType))
            {
                // Break if the next characteristic was found
                break;
            }
        }

        return (ushort)(characteristicEndIndex + 1);
    }

    /// <inheritdoc />
    public IEnumerator<GattDatabaseEntry> GetEnumerator()
    {
        lock (_lock)
        {
            return _attributes.Select((x, i) => new GattDatabaseEntry(x, (ushort)(i + 1))).GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _attributes.Count;
            }
        }
    }

    private ushort GetHandle(IGattAttribute attribute)
    {
        int index = _attributes.IndexOf(attribute);
        if (index < 0)
            throw new KeyNotFoundException("Could not find attribute");
        return (ushort)(index + 1); // Attributes start at 0x0001
    }

    /// <summary> Tries to get the handle of a given attribute </summary>
    /// <param name="attribute"> The attribute to look for </param>
    /// <param name="handle"> The attribute handle </param>
    /// <returns> True, if a handle could be found; False, otherwise </returns>
    public bool TryGetHandle(IGattAttribute attribute, out ushort handle)
    {
        lock (_lock)
        {
            int index = _attributes.IndexOf(attribute);
            if (index is < 0 or > ushort.MaxValue)
            {
                handle = 0;
                return false;
            }

            handle = (ushort)(index + 1); // Attributes start at 0x0001
            return true;
        }
    }

    /// <summary> Tries to get the attribute of a given handle </summary>
    /// <param name="handle"> The handle to get the attribute for </param>
    /// <param name="attribute"> The attribute </param>
    /// <returns> True, if an attribute could be found; False, otherwise </returns>
    public bool TryGetAttribute(ushort handle, [NotNullWhen(true)] out IGattAttribute? attribute)
    {
        lock (_lock)
        {
            int index = handle - 1;
            if (index > _attributes.Count)
            {
                attribute = null;
                return false;
            }
            attribute = _attributes[index];
            return true;
        }
    }

    /// <summary> Get the handle of a given attribute </summary>
    /// <param name="attribute"> The attribute to look for </param>
    /// <returns> The attribute handle </returns>
    /// <exception cref="KeyNotFoundException"> Thrown, if the attribute was not present in the database </exception>
    public ushort this[IGattAttribute attribute] =>
        TryGetHandle(attribute, out ushort handle) ? handle : throw new KeyNotFoundException();

    /// <summary> Get all service group entries starting from a specific handle </summary>
    /// <param name="startHandle"> The handle to start searching from </param>
    /// <returns> An enumerable containing all service group entries </returns>
    public IEnumerable<GattDatabaseGroupEntry> GetServiceEntries(ushort startHandle)
    {
        var currentIndex = (ushort)(startHandle - 1);

        lock (_lock)
        {
            while (currentIndex < _attributes.Count)
            {
                IGattAttribute attribute = _attributes[currentIndex];
                if (
                    !(
                        attribute.AttributeType.Equals(PrimaryServiceType)
                        || attribute.AttributeType.Equals(SecondaryServiceType)
                    )
                )
                {
                    currentIndex++;
                    continue;
                }

                var serviceHandle = (ushort)(currentIndex + 1);
                ushort endGroupHandle = GetServiceEndHandle(serviceHandle);
                yield return new GattDatabaseGroupEntry(attribute, serviceHandle, endGroupHandle);
                currentIndex = endGroupHandle;
            }
        }
    }

    /// <summary> Hash the gatt database </summary>
    /// <returns> The has as UInt128 </returns>
    public UInt128 CreateHash()
    {
        lock (_lock)
        {
            List<byte> bytesToHash = [];
            Span<byte> buffer = stackalloc byte[4];
            foreach (IGattAttribute gattAttribute in _attributes)
            {
                BleUuid type = gattAttribute.AttributeType;
                if (type == PrimaryServiceType || type == SecondaryServiceType || type == CharacteristicType)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], gattAttribute.Handle);
                    type.TryWriteBytes(buffer[2..4]);
                    bytesToHash.AddRange(buffer);
                    if (type == PrimaryServiceType || type == SecondaryServiceType)
                    {
                        bytesToHash.AddRange(type.ToByteArray());
                    }
                    else if (type == CharacteristicType)
                    {
                        bytesToHash.AddRange(type.ToByteArray());
                    }
                }
                else if (type == UserDescriptionType)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], gattAttribute.Handle);
                    type.TryWriteBytes(buffer[2..4]);
                    bytesToHash.AddRange(buffer);
                }
            }

            using var cmac = new AesCmac("\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"u8);
            byte[] hashBuffer = cmac.Encrypt(CollectionsMarshal.AsSpan(bytesToHash));
            return BinaryPrimitives.ReadUInt16LittleEndian(hashBuffer);
        }
    }
}
