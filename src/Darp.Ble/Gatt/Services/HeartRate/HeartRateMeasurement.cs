using System.Buffers.Binary;

namespace Darp.Ble.Gatt.Services;

/// <summary> A heart rate measurement as the Heart Rate service notifies it </summary>
/// <param name="Value"> The heart rate in beats per minute; values below 256 are sent as one byte </param>
public readonly record struct HeartRateMeasurement(ushort Value)
{
    /// <summary> The energy expended since the last reset in kilojoules; null when the sensor does not report it </summary>
    public ushort? EnergyExpended { get; init; }

    /// <summary> Whether the sensor has skin contact; null when the sensor does not detect contact </summary>
    public bool? IsSensorContactDetected { get; init; }

    /// <summary> The RR intervals in units of 1/1024 s, as sent </summary>
    public ReadOnlyMemory<ushort> RrIntervals { get; init; } = Array.Empty<ushort>();

    /// <summary> Encodes the measurement, little endian </summary>
    /// <returns> The bytes </returns>
    public byte[] ToByteArrayLittleEndian()
    {
        bool isUInt16 = Value > byte.MaxValue;
        int size = 1 + (isUInt16 ? 2 : 1) + (EnergyExpended.HasValue ? 2 : 0) + (RrIntervals.Length * 2);
        var result = new byte[size];
        var offset = 1;
        if (isUInt16)
        {
            result[0] |= 0b0000_0001;
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(offset, 2), Value);
            offset += 2;
        }
        else
        {
            result[offset++] = (byte)Value;
        }
        if (IsSensorContactDetected is { } contact)
            result[0] |= (byte)(0b0000_0100 | (contact ? 0b0000_0010 : 0));
        if (EnergyExpended is { } energy)
        {
            result[0] |= 0b0000_1000;
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(offset, 2), energy);
            offset += 2;
        }
        if (RrIntervals.Length > 0)
        {
            result[0] |= 0b0001_0000;
            foreach (ushort interval in RrIntervals.Span)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(offset, 2), interval);
                offset += 2;
            }
        }
        return result;
    }

    /// <summary> Decodes a measurement, little endian </summary>
    /// <param name="source"> The bytes </param>
    /// <returns> The measurement </returns>
    /// <exception cref="GattException"> The bytes are too short for their flags (InvalidAttributeValueLength) </exception>
    public static HeartRateMeasurement ReadLittleEndian(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
            throw TooShort();
        byte flags = source[0];
        bool isUInt16 = (flags & 0b0000_0001) != 0;
        bool hasContact = (flags & 0b0000_0010) != 0;
        bool isContactSupported = (flags & 0b0000_0100) != 0;
        bool hasEnergy = (flags & 0b0000_1000) != 0;
        bool hasRrIntervals = (flags & 0b0001_0000) != 0;
        int required = 1 + (isUInt16 ? 2 : 1) + (hasEnergy ? 2 : 0);
        if (source.Length < required || (hasRrIntervals && (source.Length - required) % 2 != 0))
            throw TooShort();
        var offset = 1;
        ushort value = isUInt16 ? BinaryPrimitives.ReadUInt16LittleEndian(source[offset..]) : source[offset];
        offset += isUInt16 ? 2 : 1;
        ushort? energy = null;
        if (hasEnergy)
        {
            energy = BinaryPrimitives.ReadUInt16LittleEndian(source[offset..]);
            offset += 2;
        }
        var intervals = new ushort[hasRrIntervals ? (source.Length - offset) / 2 : 0];
        for (var index = 0; index < intervals.Length; index++)
            intervals[index] = BinaryPrimitives.ReadUInt16LittleEndian(source[(offset + (index * 2))..]);
        return new HeartRateMeasurement(value)
        {
            EnergyExpended = energy,
            IsSensorContactDetected = isContactSupported ? hasContact : null,
            RrIntervals = intervals,
        };
    }

    private static GattException TooShort() =>
        new(GattProtocolStatus.InvalidAttributeValueLength, "The heart rate measurement is too short for its flags");
}
