using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt.Database;

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

/// <summary> The gatt database </summary>
internal sealed class GattDatabaseCollection : IGattDatabase
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

    private readonly object _lock = new();
    private readonly List<IGattAttribute> _attributes = [];

    /// <inheritdoc />
    public ushort MinHandle => 0x0001;

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void AddService(IGattClientService service)
    {
        lock (_lock)
        {
            _attributes.Add(service.Declaration);
        }
    }

    /// <inheritdoc />
    public void AddCharacteristic(IGattClientCharacteristic characteristic)
    {
        lock (_lock)
        {
            ArgumentNullException.ThrowIfNull(characteristic);
            ushort serviceHandle = GetHandle(characteristic.Service.Declaration);
            ushort serviceEndHandle = GetGroupEndHandle(serviceHandle);
            int serviceEndIndex = serviceEndHandle - 1;
            _attributes.Insert(serviceEndIndex + 1, characteristic.Declaration);
            _attributes.Insert(serviceEndIndex + 2, characteristic.Value);
        }
    }

    /// <inheritdoc />
    public void AddDescriptor(IGattClientCharacteristic characteristic, IGattCharacteristicValue descriptor)
    {
        lock (_lock)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            ushort characteristicHandle = GetHandle(characteristic.Declaration);
            ushort characteristicEndHandle = GetGroupEndHandle(characteristicHandle);
            int characteristicEndIndex = characteristicEndHandle - 1;
            _attributes.Insert(characteristicEndIndex + 1, descriptor);
        }
    }

    internal static bool IsGroupType(BleUuid attributeType) =>
        attributeType == CharacteristicType
        || attributeType == PrimaryServiceType
        || attributeType == SecondaryServiceType;

    /// <summary> Searches the database for the end of a group if the attribute has a group type. </summary>
    /// <param name="startHandle"> The start handle to start the search at </param>
    /// <returns> The end group handle if the startHandle marks a group. The start handle otherwise </returns>
    public ushort GetGroupEndHandle(ushort startHandle)
    {
        lock (_lock)
        {
            // Retrieve stoppage criterion
            if (!TryGetAttribute(startHandle, out IGattAttribute? attr))
                return startHandle;
            Func<BleUuid, bool> shouldStopPredicate;
            if (attr.AttributeType == CharacteristicType)
            {
                // If attribute is a characteristic, the group ends with any characteristic / service declaration
                shouldStopPredicate = uuid =>
                    uuid == CharacteristicType || uuid == PrimaryServiceType || uuid == SecondaryServiceType;
            }
            else if (attr.AttributeType == PrimaryServiceType || attr.AttributeType == SecondaryServiceType)
            {
                // If attribute is a service, the group ends with any service declaration
                shouldStopPredicate = uuid => uuid == PrimaryServiceType || uuid == SecondaryServiceType;
            }
            else
            {
                // The group should end immediately
                shouldStopPredicate = _ => true;
            }

            var endIndex = (ushort)(startHandle - 1);
            for (; endIndex < _attributes.Count - 1; endIndex++)
            {
                IGattAttribute attribute = _attributes[endIndex + 1];
                if (shouldStopPredicate(attribute.AttributeType))
                {
                    // Break if the stopping criterion is fulfilled
                    break;
                }
            }
            return (ushort)(endIndex + 1);
        }
    }

    /// <inheritdoc />
    public IEnumerator<GattDatabaseEntry> GetEnumerator()
    {
        lock (_lock)
        {
            return _attributes.Select((x, i) => new GattDatabaseEntry(this, x, (ushort)(i + 1))).GetEnumerator();
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public ushort this[IGattAttribute attribute] =>
        TryGetHandle(attribute, out ushort handle) ? handle : throw new KeyNotFoundException();

    /// <inheritdoc />
    public IEnumerable<GattDatabaseGroupEntry> GetServiceEntries(ushort startHandle)
    {
        var currentIndex = (ushort)(startHandle - 1);

        lock (_lock)
        {
            while (currentIndex < _attributes.Count)
            {
                IGattAttribute attribute = _attributes[currentIndex];
                if (!IsGroupType(attribute.AttributeType))
                {
                    currentIndex++;
                    continue;
                }

                var currentHandle = (ushort)(currentIndex + 1);
                ushort endGroupHandle = GetGroupEndHandle(currentHandle);
                yield return new GattDatabaseGroupEntry(attribute, currentHandle, endGroupHandle);
                currentIndex = endGroupHandle;
            }
        }
    }

    /// <inheritdoc />
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
                else if (type == DescriptorDeclaration.CharacteristicUserDescription.Uuid)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(buffer[..2], gattAttribute.Handle);
                    type.TryWriteBytes(buffer[2..4]);
                    bytesToHash.AddRange(buffer);
                }
            }

            using var cmac = new AesCmac("\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"u8);
            byte[] hashBuffer = cmac.Encrypt(CollectionsMarshal.AsSpan(bytesToHash));
            return BinaryPrimitives.ReadUInt128LittleEndian(hashBuffer);
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"GattDatabase 0x{CreateHash():X16}");
        foreach (GattDatabaseEntry entry in this)
        {
            if (entry.AttributeType == PrimaryServiceType || entry.AttributeType == SecondaryServiceType)
            {
                // Read the value.
                ValueTask<byte[]> readTask = entry.ReadValueAsync(clientPeer: null);
                byte[] value = readTask.IsCompletedSuccessfully
                    ? readTask.Result
                    : readTask.AsTask().GetAwaiter().GetResult();
                BleUuid uuid = BleUuid.Read(value);
                builder.AppendLine(CultureInfo.InvariantCulture, $"[{entry.Handle:X4}] Service {uuid}");
            }
            else if (entry.AttributeType == CharacteristicType)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"[{entry.Handle:X4}]   Characteristic");
            }
            else
            {
                builder.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"[{entry.Handle:X4}]     Attribute {entry.AttributeType}"
                );
            }
        }
        return builder.ToString();
    }
}
