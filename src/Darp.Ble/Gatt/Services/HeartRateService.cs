using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt.Services;

public readonly record struct HeartRateMeasurement(ushort Value)
{
    public ushort? EnergyExpended { get; init; }
    public bool? IsSensorContactDetected { get; init; }
    public ReadOnlyMemory<ushort> RrIntervals { get; init; } = Array.Empty<ushort>();

    public byte[] ToByteArrayLittleEndian()
    {
        var size = 1; // For the flags byte
        size += Value < 256
            ? 1
            : 2;
        if (EnergyExpended.HasValue)
        {
            size += 2;
        }
        size += RrIntervals.Length * 2;

        // Pre-allocate the array
        var result = new byte[size];
        var offset = 0;

        if (Value < 256)
        {
            result[offset++] = (byte)Value;
        }
        else
        {
            result[0] = 1; // Flag for 16-bit heart rate
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(offset, 2), (ushort)Value);
            offset += 2;
        }
        if (IsSensorContactDetected.HasValue)
        {
            result[0] |= (byte)((IsSensorContactDetected.Value ? 1 : 0) << 1);
            result[0] |= 1 << 2;
        }
        if (EnergyExpended.HasValue)
        {
            result[0] |= 1 << 3;
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(offset, 2), (ushort)EnergyExpended.Value);
            offset += 2;
        }
        if (RrIntervals.Length > 0)
        {
            result[0] |= 1 << 4;
            foreach (ushort rrInterval in RrIntervals.Span)
            {
                BitConverter.TryWriteBytes(result.AsSpan(offset, 2), (ushort)(rrInterval * 1024));
                offset += 2;
            }
        }
        return result;
    }

    public static HeartRateMeasurement ReadLittleEndian(ReadOnlySpan<byte> source)
    {
        byte flags = source[0];
        // bit 0
        bool isUInt16Format = (flags & 0b00000001) == 1;
        // bit 1
        bool hasSensorContact = (flags & 0b00000010) == 1;
        // bit 2
        bool isSensorContactSupported = (flags & 0b00000100) == 1;
        // bit 3
        bool hasEnergyExpendedField = (flags & 0b00001000) == 1;
        // bit 4
        bool hasRRIntervalValues = (flags & 0b00010000) == 1;
        var offset = 1;
        ushort value = isUInt16Format
            ? BinaryPrimitives.ReadUInt16LittleEndian(source[(offset+=2)..])
            : source[offset++];
        bool sensorContactDetected = isSensorContactSupported && hasSensorContact;
        ushort? energyExpended = hasEnergyExpendedField
            ? BinaryPrimitives.ReadUInt16LittleEndian(source[(offset+=2)..])
            : null;
        ushort[] rrIntervalValues = hasRRIntervalValues
            ? MemoryMarshal.Cast<byte, ushort>(source[offset..]).ToArray()
            : [];
        return new HeartRateMeasurement(value)
        {
            EnergyExpended = energyExpended,
            IsSensorContactDetected = sensorContactDetected,
            RrIntervals = rrIntervalValues,
        };
    }
}

public enum HeartRateBodySensorLocation : byte
{
    Other = 0,
    Chest = 1,
    Wrist = 2,
    Finger = 3,
    Hand = 4,
    EarLobe = 5,
    Foot = 6,
}

/// <summary> A class defining the heart rate service </summary>
public static class HeartRateServiceContract
{
    private const byte ResetEnergyExpended = 0x01;
    private const GattProtocolStatus ControlPointNotSupported = (GattProtocolStatus)0x80;

    /// <summary> The 16-bit UUID of the heart rate service </summary>
    public static BleUuid Uuid => new(0x180D);
    /// <summary> The 16-bit UUID of the heart rate measurement characteristic </summary>
    public static TypedCharacteristic<HeartRateMeasurement, Properties.Notify> HeartRateMeasurementCharacteristic { get; }
        = Characteristic.Create<HeartRateMeasurement, Properties.Notify>(0x2A37,
            bytes => HeartRateMeasurement.ReadLittleEndian(bytes),
            measurement => measurement.ToByteArrayLittleEndian());
    /// <summary> The 16-bit UUID of the body sensor location characteristic </summary>
    public static TypedCharacteristic<HeartRateBodySensorLocation, Properties.Read> BodySensorLocationCharacteristic { get; }
        = Characteristic.Create<HeartRateBodySensorLocation, Properties.Read>(0x2A38);

    /// <summary> The 16-bit UUID of the heart rate control point characteristic </summary>
    public static BleUuid HeartRateControlPointCharacteristicUuid { get; } = new(0x2A39);

    /// <summary> Add a heart rate service to the peripheral </summary>
    /// <param name="peripheral"> The peripheral to add the service to </param>
    /// <param name="measurementObservable"> The observable to generate measurement notifications </param>
    /// <param name="bodySensorLocation"> An optional body sensor location </param>
    /// <param name="onResetExpendedEnergy"> An optional callback to be called when an energy reset is requested </param>
    /// <param name="token"> The cancellationToken to cancel the operation </param>
    /// <returns> A task which holds a wrapper of the client service </returns>
    public static async Task<GattClientHeartRateService> AddHeartRateServiceAsync(
        this IBlePeripheral peripheral,
        IObservable<HeartRateMeasurement> measurementObservable,
        HeartRateBodySensorLocation? bodySensorLocation = null,
        Action? onResetExpendedEnergy = null,
        CancellationToken token = default
    )
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        IGattClientService service = await peripheral.AddServiceAsync(Uuid, token).ConfigureAwait(false);

        // Add the mandatory measurement characteristic
        GattTypedClientCharacteristic<HeartRateMeasurement, Properties.Notify> measurementCharacteristic = await service.AddCharacteristicAsync(
            HeartRateMeasurementCharacteristic,
            cancellationToken: token)
            .ConfigureAwait(false);
        _ = measurementObservable.Subscribe(measurement => measurementCharacteristic.NotifyAll(measurement));

        // Add the optional body sensor location
        GattTypedClientCharacteristic<HeartRateBodySensorLocation, Properties.Read>? bodySensorLocationCharacteristic = null;
        if (bodySensorLocation is not null)
        {
            bodySensorLocationCharacteristic = await service.AddCharacteristicAsync(
                BodySensorLocationCharacteristic,
                bodySensorLocation.Value,
                cancellationToken: token
            ).ConfigureAwait(false);
        }

        // Add the optional heart rate control point characteristic
        GattClientCharacteristic<Properties.Write>? heartRateControlPointCharacteristic = null;
        if (onResetExpendedEnergy is not null)
        {
            heartRateControlPointCharacteristic = await service.AddCharacteristicAsync<Properties.Write>(
                HeartRateControlPointCharacteristicUuid,
                onWrite: (_, bytes) =>
                {
                    if (bytes.Length < 1 || bytes[0] is not ResetEnergyExpended)
                        return ControlPointNotSupported;
                    onResetExpendedEnergy();
                    return GattProtocolStatus.Success;
                },
                cancellationToken: token
            ).ConfigureAwait(false);
        }

        return new GattClientHeartRateService
        {
            HeartRateMeasurement = measurementCharacteristic,
            BodySensorLocation = bodySensorLocationCharacteristic,
            HeartRateControlPoint = heartRateControlPointCharacteristic,
        };
    }

    /// <summary> The wrapper for the heart rate client service </summary>
    public sealed class GattClientHeartRateService
    {
        /// <summary> The mandatory heart rate measurement characteristic </summary>
        public required GattTypedClientCharacteristic<HeartRateMeasurement, Properties.Notify> HeartRateMeasurement { get; init; }
        /// <summary> The optional body sensor location characteristic </summary>
        public required GattTypedClientCharacteristic<HeartRateBodySensorLocation, Properties.Read>? BodySensorLocation { get; init; }
        /// <summary> The optional heart rate control point characteristic </summary>
        public required GattClientCharacteristic<Properties.Write>? HeartRateControlPoint { get; set; }
    }
}
