using System.Collections;
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
/// <summary> The gatt database </summary>
public sealed class GattDatabaseCollection : IReadOnlyDictionary<IGattAttribute, ushort>
{
    private readonly object _lock = new();
    private readonly List<IGattAttribute> _attributes = [];

    /// <summary> Add a service at the end of the database </summary>
    /// <param name="service"> The service to be added </param>
    internal void AddService(GattClientService service)
    {
        lock (_lock)
        {
            _attributes.Add(service);
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
            int serviceIndex = _attributes.IndexOf(characteristic.Service);
            if (serviceIndex < 0)
                throw new KeyNotFoundException();
            var characteristicIndex = (ushort)(serviceIndex + 1);
            for (; characteristicIndex < _attributes.Count; characteristicIndex++)
            {
                IGattAttribute attribute = _attributes[characteristicIndex];
                if (attribute is IGattClientService)
                {
                    // Break if the next service was found
                    break;
                }
            }
            _attributes.Insert(characteristicIndex, characteristic);
            _attributes.Insert(characteristicIndex + 1, new GatCharacteristicValue(characteristic));
        }
    }

    /// <summary> Add a descriptor at the end of the characteristic section </summary>
    /// <param name="descriptor"> The descriptor to be added </param>
    /// <exception cref="KeyNotFoundException"> Thrown when the characteristic the descriptor is contained in is unknown </exception>
    internal void AddDescriptor(IGattClientDescriptor descriptor)
    {
        lock (_lock)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            int characteristicIndex = _attributes.IndexOf(descriptor.Characteristic);
            if (characteristicIndex < 0)
                throw new KeyNotFoundException();
            var descriptorIndex = (ushort)(characteristicIndex + 2);
            for (; descriptorIndex < _attributes.Count; descriptorIndex++)
            {
                IGattAttribute attribute = _attributes[descriptorIndex];
                if (attribute is IGattClientCharacteristic)
                {
                    // Break if the next characteristic was found
                    break;
                }
            }
            _attributes.Insert(descriptorIndex, descriptor);
        }
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<IGattAttribute, ushort>> GetEnumerator()
    {
        lock (_lock)
        {
            return _attributes.Select((x, i) => KeyValuePair.Create(x, (ushort)i)).GetEnumerator();
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

    /// <inheritdoc />
    public bool ContainsKey(IGattAttribute key)
    {
        lock (_lock)
        {
            return _attributes.Contains(key);
        }
    }

    /// <inheritdoc />
    public bool TryGetValue(IGattAttribute key, out ushort value)
    {
        lock (_lock)
        {
            int result = _attributes.IndexOf(key);
            if (result is < 0 or > ushort.MaxValue)
            {
                value = 0;
                return false;
            }

            value = (ushort)result;
            return true;
        }
    }

    /// <inheritdoc />
    public ushort this[IGattAttribute key] =>
        TryGetValue(key, out ushort value) ? value : throw new KeyNotFoundException();

    /// <inheritdoc />
    public IEnumerable<IGattAttribute> Keys
    {
        get
        {
            lock (_lock)
            {
                return _attributes.AsReadOnly();
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<ushort> Values
    {
        get
        {
            lock (_lock)
            {
                return Enumerable.Range(0, _attributes.Count).Select(x => (ushort)x);
            }
        }
    }
}

/// <summary> The characteristic value </summary>
/// <param name="characteristic"> The characteristic </param>
public sealed class GatCharacteristicValue(IGattClientCharacteristic characteristic) : IGattAttribute
{
    private readonly IGattClientCharacteristic _characteristic = characteristic;

    /// <inheritdoc />
    public ushort Handle => _characteristic.Service.Peripheral.GattDatabase[this];

    /// <inheritdoc />
    public byte[] AttributeValue
    {
        get
        {
            ValueTask<byte[]> task = _characteristic.GetValueAsync(clientPeer: null, CancellationToken.None);
            return task.IsCompletedSuccessfully ? task.Result : task.AsTask().GetAwaiter().GetResult();
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"Characteristic Value {_characteristic.Uuid}";
}
