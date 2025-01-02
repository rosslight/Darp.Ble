using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt.Services;

public interface IHeartRateMeasurement
{
    byte[] ToByteArray();
}

public readonly record struct HeartRateMeasurement8Bit(byte Value) : IHeartRateMeasurement
{
    public ushort? EnergyExpended { get; init; }
    public bool? IsSensorContactDetected { get; init; }

    public byte[] ToByteArray()
    {
        throw new NotImplementedException();
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
    public static BleUuid HeartRateMeasurementCharacteristicUuid { get; } = new(0x2A37);
    /// <summary> The 16-bit UUID of the body sensor location characteristic </summary>
    public static BleUuid BodySensorLocationCharacteristicUuid { get; } = new(0x2A38);
    /// <summary> The 16-bit  UUID of the heart rate control point characteristic </summary>
    public static BleUuid HeartRateControlPointCharacteristicUuid { get; } = new(0x2A39);

    /// <summary> Add a heart rate service to the peripheral </summary>
    /// <param name="peripheral"> The peripheral to add the service to </param>
    /// <param name="measurementObservable"> The observable to generate measurement notifications </param>
    /// <param name="bodySensorLocation"> An optional body sensor location </param>
    /// <param name="onResetEnergyExpended"> An optional callback to be called when an energy reset is requested </param>
    /// <param name="token"> The cancellationToken to cancel the operation </param>
    /// <typeparam name="TMeasurement"> The type of the measurement </typeparam>
    /// <returns> A task which holds a wrapper of the client service </returns>
    public static async Task<GattClientHeartRateService> AddHeartRateServiceAsync<TMeasurement>(
        this IBlePeripheral peripheral,
        IObservable<TMeasurement> measurementObservable,
        HeartRateBodySensorLocation? bodySensorLocation = null,
        Action? onResetEnergyExpended = null,
        CancellationToken token = default
    )
        where TMeasurement : IHeartRateMeasurement
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        IGattClientService service = await peripheral.AddServiceAsync(Uuid, token).ConfigureAwait(false);

        // Add the mandatory measurement characteristic
        GattClientCharacteristic<Properties.Notify> measurementCharacteristic = await service.AddCharacteristicAsync<Properties.Notify>(
            HeartRateMeasurementCharacteristicUuid,
            cancellationToken: token
        ).ConfigureAwait(false);
        _ = measurementObservable.Subscribe(measurement => measurementCharacteristic.NotifyAll(measurement.ToByteArray()));

        // Add the optional body sensor location
        GattTypedClientCharacteristic<HeartRateBodySensorLocation, Properties.Read>? bodySensorLocationCharacteristic = null;
        if (bodySensorLocation is not null)
        {
            bodySensorLocationCharacteristic = await service.AddCharacteristicAsync<HeartRateBodySensorLocation, Properties.Read>(
                BodySensorLocationCharacteristicUuid,
                bodySensorLocation.Value,
                cancellationToken: token
            ).ConfigureAwait(false);
        }

        // Add the optional heart rate control point characteristic
        GattClientCharacteristic<Properties.Write>? heartRateControlPointCharacteristic = null;
        if (onResetEnergyExpended is not null)
        {
            heartRateControlPointCharacteristic = await service.AddCharacteristicAsync<Properties.Write>(
                HeartRateControlPointCharacteristicUuid,
                onWrite: (_, bytes) =>
                {
                    if (bytes.Length < 1 || bytes[0] is not ResetEnergyExpended)
                        return ControlPointNotSupported;
                    onResetEnergyExpended();
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
        public required GattClientCharacteristic<Properties.Notify> HeartRateMeasurement { get; init; }
        /// <summary> The optional body sensor location characteristic </summary>
        public required GattTypedClientCharacteristic<HeartRateBodySensorLocation, Properties.Read>? BodySensorLocation { get; init; }
        /// <summary> The optional heart rate control point characteristic </summary>
        public required GattClientCharacteristic<Properties.Write>? HeartRateControlPoint { get; set; }
    }
}
