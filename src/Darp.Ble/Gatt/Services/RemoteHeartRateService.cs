namespace Darp.Ble.Gatt.Services;

/// <summary> The heart rate service of a connected peripheral </summary>
/// <param name="HeartRateMeasurement"> The measurement </param>
/// <param name="BodySensorLocation"> The body sensor location, when the peripheral has one </param>
/// <param name="HeartRateControlPoint"> The control point, when the peripheral has one; write 0x01 to reset the expended energy </param>
public sealed record RemoteHeartRateService(
    TypedRemoteCharacteristic<HeartRateMeasurement, Properties.Notify> HeartRateMeasurement,
    TypedRemoteCharacteristic<HeartRateBodySensorLocation, Properties.Read>? BodySensorLocation,
    RemoteCharacteristic<Properties.Write>? HeartRateControlPoint
);
