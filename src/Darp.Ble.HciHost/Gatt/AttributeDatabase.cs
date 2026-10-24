using System.Buffers.Binary;
using System.Text;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gatt;

namespace Darp.Ble.HciHost.Gatt;

/// <summary>
/// The attributes this adapter serves to centrals, with their handles: the GAP service first, the GATT service if the
/// adapter includes it, then the published services. Characteristics with notify or indicate get a client
/// characteristic configuration descriptor
/// </summary>
internal sealed class AttributeDatabase
{
    private const ushort PrimaryServiceType = 0x2800;
    private const ushort CharacteristicType = 0x2803;
    private const ushort CccdType = 0x2902;

    // The attribute types the Database Hash covers, and whether their value is part of it
    private static readonly Dictionary<Guid, (ushort Type, bool WithValue)> HashedTypes = new (
        ushort Type,
        bool WithValue
    )[]
    {
        (0x2800, true),
        (0x2801, true),
        (0x2802, true),
        (0x2803, true),
        (0x2900, true),
        (0x2901, false),
        (0x2902, false),
        (0x2903, false),
        (0x2904, false),
        (0x2905, false),
    }.ToDictionary(entry => BleUuid.FromUInt16(entry.Type).Value);

    private readonly Lock _lock = new();
    private readonly Dictionary<GattCharacteristicDefinition, ushort> _valueHandles = [];
    private HostedAttribute[] _attributes = [];
    private (string DeviceName, AppearanceValues Appearance) _identity = ("Darp.Ble", AppearanceValues.Unknown);
    private byte[]? _hash;

    public AttributeDatabase(bool includeGattService)
    {
        List<HostedAttribute> gap = [];
        AddService(
            gap,
            BleUuid.FromUInt16(0x1800),
            isPrimary: true,
            build: (list, next) =>
            {
                AddStatic(list, next, 0x2A00, GattProperty.Read, () => Encoding.UTF8.GetBytes(Identity.DeviceName));
                AddStatic(
                    list,
                    next,
                    0x2A01,
                    GattProperty.Read,
                    () => BitConverter.GetBytes((ushort)Identity.Appearance)
                );
            }
        );
        if (includeGattService)
            AddGattService(gap);
        _attributes = [.. gap];
    }

    /// <summary> The handle of the Service Changed value, or null without a GATT service </summary>
    public ushort? ServiceChangedHandle { get; private set; }

    /// <summary>
    /// The GATT service: Service Changed tells centrals that subscribed when services were added, and Database Hash lets
    /// them check whether what they cached is still current
    /// </summary>
    private void AddGattService(List<HostedAttribute> attributes) =>
        AddService(
            attributes,
            BleUuid.FromUInt16(0x1801),
            isPrimary: true,
            build: (list, next) =>
            {
                var valueHandle = (ushort)(next() + 1);
                list.Add(
                    new HostedAttribute(
                        (ushort)(valueHandle - 1),
                        BleUuid.FromUInt16(CharacteristicType),
                        Declaration(GattProperty.Indicate, valueHandle, BleUuid.FromUInt16(0x2A05))
                    )
                );
                // Only indicated, never read
                list.Add(new HostedAttribute(next(), BleUuid.FromUInt16(0x2A05), hosted: null, cccdOf: null));
                ServiceChangedHandle = valueHandle;
                list.Add(
                    new HostedAttribute(next(), BleUuid.FromUInt16(CccdType), hosted: null, cccdOf: null)
                    {
                        IsServiceChangedCccd = true,
                    }
                );
                AddStatic(list, next, 0x2B2A, GattProperty.Read, Hash);
            }
        );

    /// <summary> Every attribute, in handle order: the attribute with handle h is at h - 1 </summary>
    public IReadOnlyList<HostedAttribute> Attributes => Volatile.Read(ref _attributes);

    /// <summary> What the GAP service serves </summary>
    public (string DeviceName, AppearanceValues Appearance) Identity
    {
        get
        {
            lock (_lock)
                return _identity;
        }
        set
        {
            lock (_lock)
                _identity = value;
        }
    }

    /// <summary> The attribute with the handle, or null </summary>
    public HostedAttribute? Find(ushort handle)
    {
        IReadOnlyList<HostedAttribute> attributes = Attributes;
        return handle >= 1 && handle <= attributes.Count ? attributes[handle - 1] : null;
    }

    /// <summary> The handle of a published characteristic's value </summary>
    public ushort ValueHandleOf(GattCharacteristicDefinition characteristic)
    {
        lock (_lock)
            return _valueHandles[characteristic];
    }

    /// <summary> Appends a service after the existing ones </summary>
    /// <exception cref="InvalidOperationException"> The service is the GATT service, which the adapter serves itself </exception>
    /// <exception cref="NotSupportedException"> A characteristic has an extended properties descriptor </exception>
    public void Add(GattServiceDefinition service)
    {
        if (ServiceChangedHandle is not null && service.Uuid == BleUuid.FromUInt16(0x1801))
            throw new InvalidOperationException(
                "HciHost serves the GATT service itself; set IncludeGattService = false to publish one of your own"
            );
        // Reliable writes are not supported and user descriptions are read-only, so there is nothing to extend
        if (
            service.Characteristics.Any(characteristic =>
                characteristic.Descriptors.Any(descriptor => descriptor.Uuid == BleUuid.FromUInt16(0x2900))
            )
        )
        {
            throw new NotSupportedException("HciHost serves no Characteristic Extended Properties descriptors");
        }
        lock (_lock)
        {
            List<HostedAttribute> attributes = [.. _attributes];
            AddService(
                attributes,
                service.Uuid,
                service.IsPrimary,
                (list, next) =>
                {
                    foreach (GattCharacteristicDefinition characteristic in service.Characteristics)
                    {
                        var valueHandle = (ushort)(next() + 1);
                        list.Add(
                            new HostedAttribute(
                                (ushort)(valueHandle - 1),
                                BleUuid.FromUInt16(CharacteristicType),
                                Declaration(characteristic.Properties, valueHandle, characteristic.Uuid)
                            )
                        );
                        list.Add(new HostedAttribute(next(), characteristic.Uuid, characteristic, null));
                        _valueHandles[characteristic] = valueHandle;
                        if (
                            (characteristic.Properties & (GattProperty.Notify | GattProperty.Indicate))
                            != GattProperty.None
                        )
                            list.Add(new HostedAttribute(next(), BleUuid.FromUInt16(CccdType), null, characteristic));
                        foreach (GattDescriptorDefinition descriptor in characteristic.Descriptors)
                            list.Add(new HostedAttribute(next(), descriptor.Uuid, descriptor, null));
                    }
                }
            );
            if (attributes.Count > ushort.MaxValue)
                throw new InvalidOperationException("The services need more handles than ATT has");
            Volatile.Write(ref _attributes, [.. attributes]);
            _hash = null;
        }
    }

    /// <summary>
    /// The Database Hash: AES-CMAC with a zero key over the handle, type and, for declarations, value of every service,
    /// characteristic and characteristic descriptor in handle order (Core 6.0, Vol 3, Part G, 7.3). Sent least
    /// significant byte first, like every value
    /// </summary>
    private byte[] Hash()
    {
        lock (_lock)
        {
            if (_hash is not null)
                return _hash;
            List<byte> message = [];
            Span<byte> header = stackalloc byte[4];
            foreach (HostedAttribute attribute in _attributes)
            {
                if (!HashedTypes.TryGetValue(attribute.Type.Value, out (ushort Type, bool WithValue) hashed))
                    continue;
                BinaryPrimitives.WriteUInt16LittleEndian(header, attribute.Handle);
                BinaryPrimitives.WriteUInt16LittleEndian(header[2..], hashed.Type);
                message.AddRange(header);
                if (hashed.WithValue && attribute.StaticValue is { } value)
                    message.AddRange(value);
            }
            using var cmac = new AesCmac(new byte[16]);
            byte[] hash = cmac.Encrypt(message.ToArray());
            Array.Reverse(hash);
            _hash = hash;
            return hash;
        }
    }

    private static void AddService(
        List<HostedAttribute> attributes,
        BleUuid uuid,
        bool isPrimary,
        Action<List<HostedAttribute>, Func<ushort>> build
    )
    {
        var declaration = new HostedAttribute(
            (ushort)(attributes.Count + 1),
            BleUuid.FromUInt16(isPrimary ? PrimaryServiceType : (ushort)0x2801),
            AttUuid.ToBytes(uuid)
        );
        attributes.Add(declaration);
        build(attributes, () => (ushort)(attributes.Count + 1));
        declaration.GroupEnd = (ushort)attributes.Count;
    }

    private static void AddStatic(
        List<HostedAttribute> attributes,
        Func<ushort> next,
        ushort uuid,
        GattProperty properties,
        Func<byte[]> value
    )
    {
        var valueHandle = (ushort)(next() + 1);
        attributes.Add(
            new HostedAttribute(
                (ushort)(valueHandle - 1),
                BleUuid.FromUInt16(CharacteristicType),
                Declaration(properties, valueHandle, uuid)
            )
        );
        attributes.Add(new HostedAttribute(next(), BleUuid.FromUInt16(uuid), value));
    }

    private static byte[] Declaration(GattProperty properties, ushort valueHandle, BleUuid uuid) =>
        [(byte)properties, (byte)valueHandle, (byte)(valueHandle >> 8), .. AttUuid.ToBytes(uuid)];
}
