using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;

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

public static class HeartRateServiceContract
{
    private const byte ResetEnergyExpended = 0x01;
    private const GattProtocolStatus ControlPointNotSupported = (GattProtocolStatus)0x80;

    public static BleUuid Uuid => new(0x180D);
    public static Characteristic<Properties.Notify> HeartRateMeasurementCharacteristic { get; } = new(0x2A37);
    public static Characteristic<Properties.Read> BodySensorLocationCharacteristic { get; } = new(0x2A38);
    public static Characteristic<Properties.Write> HeartRateControlPointCharacteristic { get; } = new(0x2A39);

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
            HeartRateMeasurementCharacteristic.Uuid,
            cancellationToken: token
        ).ConfigureAwait(false);
        _ = measurementObservable.Subscribe(measurement => measurementCharacteristic.NotifyAll(measurement.ToByteArray()));

        // Add the optional body sensor location
        GattTypedClientCharacteristic<HeartRateBodySensorLocation, Properties.Read>? bodySensorLocationCharacteristic = null;
        if (bodySensorLocation is not null)
        {
            bodySensorLocationCharacteristic = await service.AddCharacteristicAsync<HeartRateBodySensorLocation, Properties.Read>(
                BodySensorLocationCharacteristic.Uuid,
                bodySensorLocation.Value,
                cancellationToken: token
            ).ConfigureAwait(false);
        }

        // Add the optional heart rate control point characteristic
        GattClientCharacteristic<Properties.Write>? heartRateControlPointCharacteristic = null;
        if (onResetEnergyExpended is not null)
        {
            heartRateControlPointCharacteristic = await service.AddCharacteristicAsync<Properties.Write>(
                HeartRateControlPointCharacteristic.Uuid,
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

    public static async Task<GattServerWcpService> DiscoverWcpServiceAsync(
        this IGattServerPeer serverPeer,
        CancellationToken cancellationToken = default
    )
    {
        IGattServerService service = await serverPeer.DiscoverServiceAsync(Uuid, cancellationToken);
        IGattServerCharacteristic<Properties.Write> char1 = await service.DiscoverCharacteristicAsync(
            Write,
            cancellationToken: cancellationToken
        );
        IGattServerCharacteristic<Properties.Notify> char2 = await service.DiscoverCharacteristicAsync(
            Notify,
            cancellationToken: cancellationToken
        );
        return new GattServerWcpService { Write = char1, Notify = char2 };
    }

    public sealed class GattClientHeartRateService
    {
        public required GattClientCharacteristic<Properties.Notify> HeartRateMeasurement { get; init; }
        public required GattTypedClientCharacteristic<HeartRateBodySensorLocation, Properties.Read>? BodySensorLocation { get; init; }
        public required GattClientCharacteristic<Properties.Write>? HeartRateControlPoint { get; set; }
    }

    public sealed class GattServerWcpService
    {
        public required IGattServerCharacteristic<Properties.Notify> Notify { get; init; }
        public required IGattServerCharacteristic<Properties.Write> Write { get; init; }
    }
}
