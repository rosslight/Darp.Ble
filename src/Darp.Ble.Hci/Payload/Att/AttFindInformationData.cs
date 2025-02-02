using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The find information data </summary>
/// <param name="Handle"> The Handle </param>
/// <param name="Uuid"> The Bluetooth UUID bytes </param>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-9e854de5-e5b8-5a40-d07e-6d69be117dad_informaltable-idm13358909072592"/>
[BinaryObject]
public readonly partial record struct AttFindInformationData(ushort Handle, ReadOnlyMemory<byte> Uuid);
