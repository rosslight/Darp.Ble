using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;

namespace Darp.Ble.WinRT.Gap;

/// <summary> Windows' advertisement publisher sets some data sections itself and refuses them from the app </summary>
/// <seealso href="https://learn.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.advertisement.bluetoothleadvertisementpublisher"/>
internal static class PublisherSections
{
    private static readonly HashSet<AdTypes> Reserved =
    [
        AdTypes.Flags,
        AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids,
        AdTypes.CompleteListOf16BitServiceOrServiceClassUuids,
        AdTypes.IncompleteListOf32BitServiceOrServiceClassUuids,
        AdTypes.CompleteListOf32BitServiceOrServiceClassUuids,
        AdTypes.IncompleteListOf128BitServiceOrServiceClassUuids,
        AdTypes.CompleteListOf128BitServiceOrServiceClassUuids,
        AdTypes.ShortenedLocalName,
        AdTypes.CompleteLocalName,
        AdTypes.TxPowerLevel,
        AdTypes.ClassOfDevice,
        AdTypes.SimplePairingHashC192,
        AdTypes.SimplePairingRandomizerR192,
        AdTypes.SecurityManagerTkValue,
        AdTypes.SecurityManagerOutOfBandFlags,
        AdTypes.PeripheralConnectionIntervalRange,
        AdTypes.ListOf16BitServiceSolicitationUuids,
        AdTypes.ListOf32BitServiceSolicitationUuids,
        AdTypes.ListOf128BitServiceSolicitationUuids,
        AdTypes.ServiceData16BitUuid,
        AdTypes.ServiceData32BitUuid,
        AdTypes.ServiceData128BitUuid,
        AdTypes.PublicTargetAddress,
        AdTypes.RandomTargetAddress,
        AdTypes.Appearance,
        AdTypes.AdvertisingInterval,
        AdTypes.LeBluetoothDeviceAddress,
        AdTypes.LeRole,
        AdTypes.SimplePairingHashC256,
        AdTypes.SimplePairingRandomizerR256,
        AdTypes.ThreeDInformationData,
    ];

    /// <summary> The sections the publisher takes, and the types it would refuse </summary>
    public static (
        IReadOnlyList<(AdTypes Type, ReadOnlyMemory<byte> Bytes)> Kept,
        IReadOnlyList<AdTypes> Dropped
    ) Split(AdvertisingData data)
    {
        List<(AdTypes, ReadOnlyMemory<byte>)> kept = [];
        List<AdTypes> dropped = [];
        foreach ((AdTypes type, ReadOnlyMemory<byte> bytes) in data)
        {
            if (Reserved.Contains(type))
                dropped.Add(type);
            else
                kept.Add((type, bytes));
        }
        return (kept, dropped);
    }
}
