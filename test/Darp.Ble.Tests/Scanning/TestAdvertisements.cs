using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Tests.Scanning;

/// <summary> Advertisements for tests at the observer driver boundary </summary>
internal static class TestAdvertisements
{
    private static readonly Dictionary<byte, IGapAdvertisement> Advertisements = [];

    /// <summary> One advertisement per number, the same instance each time, so assertions can compare references </summary>
    public static IGapAdvertisement Get(byte number)
    {
        lock (Advertisements)
        {
            if (!Advertisements.TryGetValue(number, out IGapAdvertisement? advertisement))
            {
                advertisement = GapAdvertisement.FromExtendedAdvertisingReport(
                    DateTimeOffset.UnixEpoch,
                    BleEventType.AdvNonConnInd,
                    new BleAddress(BleAddressType.Public, (UInt48)number),
                    Physical.Le1M,
                    Physical.NotAvailable,
                    AdvertisingSId.NoAdIProvided,
                    TxPowerLevel.NotAvailable,
                    (Rssi)(-50),
                    PeriodicAdvertisingInterval.NoPeriodicAdvertising,
                    BleAddress.NotAvailable,
                    AdvertisingData.Empty
                );
                Advertisements[number] = advertisement;
            }
            return advertisement;
        }
    }
}
