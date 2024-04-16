namespace Darp.Ble.Data;

public enum GattProtocolStatus : byte
{
    Success = 0x00,
    WriteRequestRejected = 0xFC,
    ClientCharacteristicConfigurationDescriptorImproperlyConfigured = 0xFD,
    ProcedureAlreadyInProgress = 0xFE,
    OutOfRange = 0xFF,
}