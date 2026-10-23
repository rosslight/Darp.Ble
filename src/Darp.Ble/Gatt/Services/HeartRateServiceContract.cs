using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Services;

/// <summary> The Heart Rate service </summary>
/// <seealso href="https://www.bluetooth.com/specifications/specs/heart-rate-service-1-0/"/>
public static class HeartRateServiceContract
{
    /// <summary> The control point command that resets the expended energy </summary>
    private const byte ResetEnergyExpended = 0x01;

    /// <summary> The service's error for an unknown control point command </summary>
    private const GattProtocolStatus ControlPointNotSupported = (GattProtocolStatus)0x80;

    /// <summary> The declaration of the service, 0x180D </summary>
    public static ServiceDeclaration HeartRateService { get; } = new(0x180D);

    /// <summary> The heart rate measurement, 0x2A37 </summary>
    public static TypedCharacteristicDeclaration<
        HeartRateMeasurement,
        Properties.Notify
    > HeartRateMeasurementCharacteristic { get; } =
        CharacteristicDeclaration.Create<HeartRateMeasurement, Properties.Notify>(
            0x2A37,
            HeartRateMeasurement.ReadLittleEndian,
            measurement => measurement.ToByteArrayLittleEndian()
        );

    /// <summary> The body sensor location, 0x2A38 </summary>
    public static TypedCharacteristicDeclaration<
        HeartRateBodySensorLocation,
        Properties.Read
    > BodySensorLocationCharacteristic { get; } =
        CharacteristicDeclaration.Create<HeartRateBodySensorLocation, Properties.Read>(0x2A38);

    /// <summary> The heart rate control point, 0x2A39 </summary>
    public static CharacteristicDeclaration<Properties.Write> HeartRateControlPointCharacteristic { get; } =
        new(0x2A39);

    /// <summary> Publishes a heart rate service </summary>
    /// <param name="gatt"> The adapter's services </param>
    /// <param name="bodySensorLocation"> The body sensor location; the characteristic is left out when null </param>
    /// <param name="onResetExpendedEnergy"> Called when a central resets the expended energy; the control point is left out when null </param>
    /// <param name="cancellationToken"> Cancels publishing </param>
    /// <returns> The service </returns>
    public static async Task<LocalHeartRateService> AddHeartRateServiceAsync(
        this GattServer gatt,
        HeartRateBodySensorLocation? bodySensorLocation = null,
        Action? onResetExpendedEnergy = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(gatt);
        var builder = new LocalGattServiceBuilder(HeartRateService);
        TypedLocalCharacteristic<HeartRateMeasurement, Properties.Notify> measurement = builder.AddCharacteristic(
            HeartRateMeasurementCharacteristic
        );
        TypedLocalCharacteristic<HeartRateBodySensorLocation, Properties.Read>? location = bodySensorLocation
            is { } value
            ? builder.AddCharacteristic(BodySensorLocationCharacteristic, onRead: (_, _) => ValueTask.FromResult(value))
            : null;
        LocalCharacteristic<Properties.Write>? controlPoint = onResetExpendedEnergy is null
            ? null
            : builder.AddCharacteristic(
                HeartRateControlPointCharacteristic,
                onWrite: (_, command, _) =>
                {
                    if (command.Length != 1 || command.Span[0] is not ResetEnergyExpended)
                        return ValueTask.FromResult(ControlPointNotSupported);
                    onResetExpendedEnergy();
                    return ValueTask.FromResult(GattProtocolStatus.Success);
                }
            );
        await gatt.AddServiceAsync(builder, cancellationToken).ConfigureAwait(false);
        return new LocalHeartRateService(measurement, location, controlPoint);
    }

    /// <summary> Discovers the heart rate service of a connected peripheral </summary>
    /// <param name="gatt"> The peripheral's services </param>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The service's characteristics; the optional ones are null when the peripheral lacks them </returns>
    public static async Task<RemoteHeartRateService> DiscoverHeartRateServiceAsync(
        this GattClient gatt,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(gatt);
        RemoteGattService service = await gatt.DiscoverServiceAsync(HeartRateService, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await service
            .DiscoverCharacteristicsAsync(cancellationToken)
            .ConfigureAwait(false);
        RemoteGattCharacteristic measurement = RemoteGattServiceExtensions.Require(
            service,
            characteristics,
            HeartRateMeasurementCharacteristic
        );
        RemoteGattCharacteristic? location = RemoteGattServiceExtensions.Match(
            characteristics,
            BodySensorLocationCharacteristic
        );
        RemoteGattCharacteristic? controlPoint = RemoteGattServiceExtensions.Match(
            characteristics,
            HeartRateControlPointCharacteristic
        );
        return new RemoteHeartRateService(
            new TypedRemoteCharacteristic<HeartRateMeasurement, Properties.Notify>(
                measurement,
                HeartRateMeasurementCharacteristic
            ),
            location is null
                ? null
                : new TypedRemoteCharacteristic<HeartRateBodySensorLocation, Properties.Read>(
                    location,
                    BodySensorLocationCharacteristic
                ),
            controlPoint is null ? null : new RemoteCharacteristic<Properties.Write>(controlPoint)
        );
    }
}
