using System.Runtime.InteropServices;
using Darp.BinaryObjects;

namespace Darp.Ble.Hci.Payload.Att;

/// <summary> The group type response data </summary>
/// <param name="Handle"> The Attribute Handle </param>
/// <param name="EndGroup"> The End Group Handle </param>
/// <param name="Value"> The Attribute Value </param>
/// <typeparam name="TAttributeValue"> The type of the value </typeparam>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-3ca57165-f2ce-1531-4583-95d33d899fff_table-idm13358909789874"/>
[BinaryObject]
public readonly partial record struct AttGroupTypeData<TAttributeValue>(ushort Handle, ushort EndGroup, TAttributeValue Value)
    where TAttributeValue : unmanaged;