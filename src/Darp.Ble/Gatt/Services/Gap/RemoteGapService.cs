using Darp.Ble.Data.AssignedNumbers;

namespace Darp.Ble.Gatt.Services;

/// <summary> The GAP service of a connected peripheral </summary>
/// <param name="DeviceName"> The device name </param>
/// <param name="Appearance"> The appearance </param>
public sealed record RemoteGapService(
    TypedRemoteCharacteristic<string, Properties.Read> DeviceName,
    TypedRemoteCharacteristic<AppearanceValues, Properties.Read> Appearance
);
