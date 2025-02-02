namespace Darp.Ble.Hci.Payload.Result;

/// <summary> The Hci LE Features </summary>
[Flags]
public enum HciLeFeatures : ulong
{
    /// <summary> None </summary>
    None = 0,

    /// <summary> LeEncryption </summary>
    LeEncryption = 1 << 0,

    /// <summary> ConnectionParametersRequestProcedure </summary>
    ConnectionParametersRequestProcedure = 1 << 1,

    /// <summary> ExtendedRejectIndication </summary>
    ExtendedRejectIndication = 1 << 2,

    /// <summary> PeripheralInitiatedFeaturesExchange </summary>
    PeripheralInitiatedFeaturesExchange = 1 << 3,

    /// <summary> LePing </summary>
    LePing = 1 << 4,

    /// <summary> LeDataPacketLengthExtension </summary>
    LeDataPacketLengthExtension = 1 << 5,

    /// <summary> LlPrivacy </summary>
    LlPrivacy = 1 << 6,

    /// <summary> ExtendedScanningFilterPolicies </summary>
    ExtendedScanningFilterPolicies = 1 << 7,

    /// <summary> Le2MPHY </summary>
    Le2Mphy = 1 << 8,

    /// <summary> StableModulationIndexTransmitter </summary>
    StableModulationIndexTransmitter = 1 << 9,

    /// <summary> StableModulationIndexReceiver </summary>
    StableModulationIndexReceiver = 1 << 10,

    /// <summary> LeCodedPHY </summary>
    LeCodedPhy = 1 << 11,

    /// <summary> LeExtendedAdvertising </summary>
    LeExtendedAdvertising = 1 << 12,

    /// <summary> LePeriodicAdvertising </summary>
    LePeriodicAdvertising = 1 << 13,

    /// <summary> ChannelSelectionAlgorithm2 </summary>
    ChannelSelectionAlgorithm2 = 1 << 14,

    /// <summary> LePowerClass1 </summary>
    LePowerClass1 = 1 << 15,

    /// <summary> MinimumNumberOfUsedChannelsProcedure </summary>
    MinimumNumberOfUsedChannelsProcedure = 1 << 16,

    /// <summary> ConnectionCTERequest </summary>
    ConnectionCteRequest = 1 << 17,

    /// <summary> ConnectionCTEResponse </summary>
    ConnectionCteResponse = 1 << 18,

    /// <summary> ConnectionlessCTETransmitter </summary>
    ConnectionlessCteTransmitter = 1 << 19,

    /// <summary> ConnectionlessCTEReceiver </summary>
    ConnectionlessCteReceiver = 1 << 20,
}
