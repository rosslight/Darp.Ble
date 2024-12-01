using System.Runtime.InteropServices;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The handles information </summary>
/// <param name="FoundAttributeHandle"> The Found Attribute Handle </param>
/// <param name="GroupEndHandle"> The Group End Handle </param>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-ca00cb2c-f48c-1a01-175a-31d20d7fd3d0_informaltable-idm13358909132944"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct AttFindByTypeHandlesInformation(ushort FoundAttributeHandle, ushort GroupEndHandle);
