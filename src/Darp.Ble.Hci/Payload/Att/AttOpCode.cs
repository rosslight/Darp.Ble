using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary>
/// Attribute Opcode summary
/// </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-915a47c5-8e31-f97d-ce9c-e24fa4406014_table-idm13358910852784"/>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public enum AttOpCode : byte
{
    /// <summary> Invalid OpCode </summary>
    None,

    /// <summary> The <see cref="AttErrorRsp"/> </summary>
    ATT_ERROR_RSP = 0x01,

    /// <summary> The <see cref="AttExchangeMtuReq"/> </summary>
    ATT_EXCHANGE_MTU_REQ = 0x02,

    /// <summary> The <see cref="AttExchangeMtuRsp"/> </summary>
    ATT_EXCHANGE_MTU_RSP = 0x03,

    /// <summary> The <see cref="AttFindInformationReq"/> </summary>
    ATT_FIND_INFORMATION_REQ = 0x04,

    /// <summary> The <see cref="AttFindInformationRsp"/> </summary>
    ATT_FIND_INFORMATION_RSP = 0x05,

    /// <summary> The <see cref="AttFindByTypeValueReq"/> </summary>
    ATT_FIND_BY_TYPE_VALUE_REQ = 0x06,

    /// <summary> The <see cref="AttFindByTypeValueRsp"/> </summary>
    ATT_FIND_BY_TYPE_VALUE_RSP = 0x07,

    /// <summary> The <see cref="AttReadByTypeReq{TAttributeType}"/> </summary>
    ATT_READ_BY_TYPE_REQ = 0x08,

    /// <summary> The <see cref="AttReadByTypeRsp"/> </summary>
    ATT_READ_BY_TYPE_RSP = 0x09,

    /// <summary> The <see cref="AttReadByGroupTypeReq{TAttributeType}"/> </summary>
    ATT_READ_BY_GROUP_TYPE_REQ = 0x10,

    /// <summary> The <see cref="AttReadByGroupTypeRsp{TAttributeValue}"/> </summary>
    ATT_READ_BY_GROUP_TYPE_RSP = 0x11,

    /// <summary> The <see cref="AttWriteReq"/> </summary>
    ATT_WRITE_REQ = 0x12,

    /// <summary> The <see cref="AttWriteRsp"/> </summary>
    ATT_WRITE_RSP = 0x13,

    /// <summary> The prepare write req </summary>
    ATT_PREPARE_WRITE_REQ = 0x16,

    /// <summary> The prepare write rsp </summary>
    ATT_PREPARE_WRITE_RSP = 0x17,

    /// <summary> The execute write req </summary>
    ATT_EXECUTE_WRITE_REQ = 0x18,

    /// <summary> The execute write rsp </summary>
    ATT_EXECUTE_WRITE_RSP = 0x19,

    /// <summary> The <see cref="AttWriteCmd"/> </summary>
    ATT_WRITE_CMD = 0x52,

    /// <summary> The <see cref="AttHandleValueNtf"/> </summary>
    ATT_HANDLE_VALUE_NTF = 0x1B,
}
