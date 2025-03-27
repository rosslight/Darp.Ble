using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Gatt.Services;

/// <summary> The heart rate measurement </summary>
/// <param name="Value"> The heart rate value. Values fitting into a byte will be transmitted as such </param>
public readonly record struct HeartRateMeasurement(ushort Value)
{
    /// <summary> The expended energy in this cycle. A null value indicates that this feature is disabled </summary>
    public ushort? EnergyExpended { get; init; }

    /// <summary> The body sensor contact detection flag. A null value indicates that this feature is disabled </summary>
    public bool? IsSensorContactDetected { get; init; }

    /// <summary> A list of all RR Interval </summary>
    public ReadOnlyMemory<ushort> RrIntervals { get; init; } = Array.Empty<ushort>();

    /// <summary> Write the measurement as a byte array in little endian </summary>
    /// <returns> The resulting byte array </returns>
    public byte[] ToByteArrayLittleEndian()
    {
        var size = 1; // For the flags byte
        size += Value < 256 ? 1 : 2;
        if (EnergyExpended.HasValue)
        {
            size += 2;
        }
        size += RrIntervals.Length * 2;

        // Pre-allocate the array
        var result = new byte[size];
        var offset = 1;

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
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(offset, 2), EnergyExpended.Value);
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

    /// <summary> Read the heart rate measurement from a span of bytes in little endian </summary>
    /// <param name="source"> The source to read from </param>
    /// <returns> The heart rate measurement </returns>
    public static HeartRateMeasurement ReadLittleEndian(ReadOnlySpan<byte> source)
    {
        byte flags = source[0];
        // bit 0
        bool isUInt16Format = (flags & 0b00000001) == 1;
        // bit 1
        bool hasSensorContact = (flags & 0b00000010) == 0b00000010;
        // bit 2
        bool isSensorContactSupported = (flags & 0b00000100) == 0b00000100;
        // bit 3
        bool hasEnergyExpendedField = (flags & 0b00001000) == 0b00001000;
        // bit 4
        bool hasRrIntervalValues = (flags & 0b00010000) == 0b00010000;
        var offset = 1;
        ushort value = isUInt16Format
            ? BinaryPrimitives.ReadUInt16LittleEndian(source[((offset += 2) - 2)..])
            : source[offset++];
        bool sensorContactDetected = isSensorContactSupported && hasSensorContact;
        ushort? energyExpended = hasEnergyExpendedField
            ? BinaryPrimitives.ReadUInt16LittleEndian(source[((offset += 2) - 2)..])
            : null;
        ushort[] rrIntervalValues = hasRrIntervalValues
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

/// <summary> The body sensor location </summary>
public enum HeartRateBodySensorLocation : byte
{
    /// <summary> Unknown location </summary>
    Other = 0,

    /// <summary> Chest location </summary>
    Chest = 1,

    /// <summary> Wrist location </summary>
    Wrist = 2,

    /// <summary> Finger location </summary>
    Finger = 3,

    /// <summary> Hand location </summary>
    Hand = 4,

    /// <summary> EarLobe location </summary>
    EarLobe = 5,

    /// <summary> Foot location </summary>
    Foot = 6,
}

/// <summary> A class defining the heart rate service </summary>
public static class HeartRateServiceContract
{
    private const byte ResetEnergyExpended = 0x01;
    private const GattProtocolStatus ControlPointNotSupported = (GattProtocolStatus)0x80;

    /// <summary> The 16-bit UUID of the heart rate service </summary>
    public static ServiceDeclaration HeartRateService => new(0x180D);

    /// <summary> The 16-bit UUID of the heart rate measurement characteristic </summary>
    public static TypedCharacteristicDeclaration<
        HeartRateMeasurement,
        Properties.Notify
    > HeartRateMeasurementCharacteristic { get; } =
        CharacteristicDeclaration.Create<HeartRateMeasurement, Properties.Notify>(
            0x2A37,
            HeartRateMeasurement.ReadLittleEndian,
            measurement => measurement.ToByteArrayLittleEndian()
        );

    /// <summary> The 16-bit UUID of the body sensor location characteristic </summary>
    public static TypedCharacteristicDeclaration<
        HeartRateBodySensorLocation,
        Properties.Read
    > BodySensorLocationCharacteristic { get; } =
        CharacteristicDeclaration.Create<HeartRateBodySensorLocation, Properties.Read>(0x2A38);

    /// <summary> The 16-bit UUID of the heart rate control point characteristic </summary>
    public static CharacteristicDeclaration<Properties.Write> HeartRateControlPointCharacteristic { get; } =
        new(0x2A39);

    /// <summary> Add a heart rate service to the peripheral </summary>
    /// <param name="peripheral"> The peripheral to add the service to </param>
    /// <param name="bodySensorLocation"> An optional body sensor location </param>
    /// <param name="onResetExpendedEnergy"> An optional callback to be called when an energy reset is requested </param>
    /// <returns> A task which holds a wrapper of the client service </returns>
    public static GattClientHeartRateService AddHeartRateService(
        this IBlePeripheral peripheral,
        HeartRateBodySensorLocation? bodySensorLocation = null,
        Action? onResetExpendedEnergy = null
    )
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        IGattClientService service = peripheral.AddService(HeartRateService);

        // Add the mandatory measurement characteristic
        GattTypedClientCharacteristic<HeartRateMeasurement, Properties.Notify> measurementCharacteristic =
            service.AddCharacteristic(HeartRateMeasurementCharacteristic);

        // Add the optional body sensor location
        GattTypedClientCharacteristic<HeartRateBodySensorLocation, Properties.Read>? bodySensorLocationCharacteristic =
            null;
        if (bodySensorLocation is not null)
        {
            bodySensorLocationCharacteristic = service.AddCharacteristic(
                BodySensorLocationCharacteristic,
                bodySensorLocation.Value
            );
        }

        // Add the optional heart rate control point characteristic
        GattClientCharacteristic<Properties.Write>? heartRateControlPointCharacteristic = null;
        if (onResetExpendedEnergy is not null)
        {
            heartRateControlPointCharacteristic = service.AddCharacteristic<Properties.Write>(
                HeartRateControlPointCharacteristic.Uuid,
                onWrite: (_, bytes, _) =>
                {
                    if (bytes.Length < 1 || bytes[0] is not ResetEnergyExpended)
                        return ControlPointNotSupported;
                    onResetExpendedEnergy();
                    return GattProtocolStatus.Success;
                }
            );
        }

        return new GattClientHeartRateService(service)
        {
            HeartRateMeasurement = measurementCharacteristic,
            BodySensorLocation = bodySensorLocationCharacteristic,
            HeartRateControlPoint = heartRateControlPointCharacteristic,
        };
    }

    /// <summary> Discover the echo server </summary>
    /// <param name="serverPeer"> The peer to discover the service from </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> A wrapper with the discovered characteristics </returns>
    public static async Task<GattServerHeartRateService> DiscoverHeartRateServiceAsync(
        this IGattServerPeer serverPeer,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(serverPeer);

        // Discover the service
        IGattServerService service = await serverPeer
            .DiscoverServiceAsync(HeartRateService, cancellationToken)
            .ConfigureAwait(false);
        await service.DiscoverCharacteristicsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (
            !service.TryGetCharacteristic(
                HeartRateMeasurementCharacteristic,
                out TypedGattServerCharacteristic<HeartRateMeasurement, Properties.Notify>? measurementCharacteristic
            )
        )
        {
            throw new Exception("HeartRateMeasurement characteristic is not contained in service");
        }

        service.TryGetCharacteristic(
            BodySensorLocationCharacteristic,
            out TypedGattServerCharacteristic<
                HeartRateBodySensorLocation,
                Properties.Read
            >? bodySensorLocationCharacteristic
        );
        service.TryGetCharacteristic(
            HeartRateControlPointCharacteristic,
            out GattServerCharacteristic<Properties.Write>? heartRateControlPointCharacteristic
        );

        return new GattServerHeartRateService(service)
        {
            HeartRateMeasurement = measurementCharacteristic,
            BodySensorLocation = bodySensorLocationCharacteristic,
            HeartRateControlPoint = heartRateControlPointCharacteristic,
        };
    }
}

/// <summary> The wrapper for the heart rate client service </summary>
public sealed class GattClientHeartRateService(IGattClientService service) : GattClientServiceProxy(service)
{
    /// <summary> The mandatory heart rate measurement characteristic </summary>
    public required GattTypedClientCharacteristic<
        HeartRateMeasurement,
        Properties.Notify
    > HeartRateMeasurement { get; init; }

    /// <summary> The optional body sensor location characteristic </summary>
    public required GattTypedClientCharacteristic<
        HeartRateBodySensorLocation,
        Properties.Read
    >? BodySensorLocation { get; init; }

    /// <summary> The optional heart rate control point characteristic </summary>
    public required GattClientCharacteristic<Properties.Write>? HeartRateControlPoint { get; init; }
}

/// <summary> The HeartRate wrapper representing the gatt server </summary>
public sealed class GattServerHeartRateService(IGattServerService service) : GattServerServiceProxy(service)
{
    /// <summary> The write characteristic </summary>
    public required TypedGattServerCharacteristic<
        HeartRateMeasurement,
        Properties.Notify
    > HeartRateMeasurement { get; init; }

    /// <summary> The optional body sensor location characteristic </summary>
    public required TypedGattServerCharacteristic<
        HeartRateBodySensorLocation,
        Properties.Read
    >? BodySensorLocation { get; init; }

    /// <summary> The optional heart rate control point characteristic </summary>
    public required GattServerCharacteristic<Properties.Write>? HeartRateControlPoint { get; init; }
}
