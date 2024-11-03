using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The find information data </summary>
/// <param name="Handle"> The Handle </param>
/// <param name="Uuid"> The 16-bit Bluetooth UUID </param>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-9e854de5-e5b8-5a40-d07e-6d69be117dad_informaltable-idm13358909072592"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct AttFindInformationData(ushort Handle, ushort Uuid);