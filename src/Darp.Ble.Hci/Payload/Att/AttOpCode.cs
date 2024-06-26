using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Hci.Payload.Att;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum AttOpCode
{
    ATT_ERROR_RSP = 0x01,
    ATT_EXCHANGE_MTU_REQ = 0x02,
    ATT_EXCHANGE_MTU_RSP = 0x03,
    ATT_FIND_INFORMATION_REQ = 0x04,
    ATT_FIND_INFORMATION_RSP = 0x05,
    ATT_FIND_BY_TYPE_VALUE_REQ = 0x06,
    ATT_FIND_BY_TYPE_VALUE_RSP = 0x07,
    ATT_READ_BY_TYPE_REQ = 0x08,
    ATT_READ_BY_TYPE_RSP = 0x09,
    ATT_READ_BY_GROUP_TYPE_REQ = 0x10,
    ATT_READ_BY_GROUP_TYPE_RSP = 0x11,
    ATT_WRITE_REQ = 0x12,
    ATT_WRITE_RSP = 0x13,

    ATT_PREPARE_WRITE_REQ = 0x16,
    ATT_PREPARE_WRITE_RSP = 0x17,
    ATT_EXECUTE_WRITE_REQ = 0x18,
    ATT_EXECUTE_WRITE_RSP = 0x19,

    ATT_WRITE_CMD = 0x52,

    ATT_HANDLE_VALUE_NTF = 0x1B,
}