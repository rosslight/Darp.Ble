namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The att type data </summary>
/// <param name="Handle"> Attribute Handle </param>
/// <param name="Value"> Attribute Value </param>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-d733ce4b-61cb-eca2-ba74-4a13bbf4d349_informaltable-idm13358909202896"/>
public readonly record struct AttReadByTypeData(ushort Handle, byte[] Value);