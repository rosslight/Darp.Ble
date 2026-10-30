using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Mock.Gap;

/// <summary> What an advertising set sends, as one consistent snapshot </summary>
internal sealed record MockAdvertisingState(
    AdvertisingParameters Parameters,
    BleAddress Address,
    AdvertisingData Data,
    AdvertisingData? ScanResponseData
);
