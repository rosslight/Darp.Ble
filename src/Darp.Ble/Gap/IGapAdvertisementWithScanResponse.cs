namespace Darp.Ble.Gap;

/// <summary> The definition of an advertisement with an associated scan response </summary>
public interface IGapAdvertisementWithScanResponse : IGapAdvertisement
{
    /// <summary> The scan response associated with the advertisement </summary>
    IGapAdvertisement ScanResponse { get; }
}
