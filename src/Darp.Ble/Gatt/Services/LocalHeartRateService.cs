namespace Darp.Ble.Gatt.Services;

/// <summary> A heart rate service the adapter hosts </summary>
/// <param name="HeartRateMeasurement"> The measurement; notify it with NotifyAsync or NotifyAllAsync </param>
/// <param name="BodySensorLocation"> The body sensor location, when the service has one </param>
/// <param name="HeartRateControlPoint"> The control point, when the service has one </param>
public sealed record LocalHeartRateService(
    TypedLocalCharacteristic<HeartRateMeasurement, Properties.Notify> HeartRateMeasurement,
    TypedLocalCharacteristic<HeartRateBodySensorLocation, Properties.Read>? BodySensorLocation,
    LocalCharacteristic<Properties.Write>? HeartRateControlPoint
);
