using System.Text;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gatt;

namespace Darp.Ble.HciHost.Gatt;

/// <summary>
/// The attributes this adapter serves to centrals, with their handles: the GAP service first, then the added services.
/// Characteristics with notify or indicate get a client characteristic configuration descriptor
/// </summary>
internal sealed class AttributeDatabase
{
    private const ushort PrimaryServiceType = 0x2800;
    private const ushort CharacteristicType = 0x2803;
    private const ushort CccdType = 0x2902;

    private readonly Lock _lock = new();
    private readonly Dictionary<GattCharacteristicDefinition, ushort> _valueHandles = [];
    private HostedAttribute[] _attributes = [];
    private (string DeviceName, AppearanceValues Appearance) _identity = ("Darp.Ble", AppearanceValues.Unknown);

    public AttributeDatabase()
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
        _attributes = [.. gap];
    }

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

    /// <summary> The handle of an added characteristic's value </summary>
    public ushort ValueHandleOf(GattCharacteristicDefinition characteristic)
    {
        lock (_lock)
            return _valueHandles[characteristic];
    }

    /// <summary> Appends a service after the existing ones </summary>
    public void Add(GattServiceDefinition service)
    {
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
