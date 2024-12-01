namespace Darp.Ble.Hci.Package;

/// <summary> The hci group field </summary>
public enum HciOpCodeGroupField : byte
{
    /// <summary> An invalid group field </summary>
    None,
    /// <summary> The Link Control commands allow a Controller to control connections to other BR/EDR Controllers </summary>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-7ae7f387-3f16-9fd8-fb80-1b957b3a4c4d"/>
    LinkControl = 0x01,
    /// <summary> The Controller and Baseband commands provide access and control to various capabilities of the Bluetooth hardware </summary>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-ded9b390-e137-1e8b-9527-f64b9bedbf27"/>
    Controller = 0x03,
    /// <summary> The informational parameters are fixed by the manufacturer of the Bluetooth hardware </summary>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-5077121c-5721-5162-5d45-1c46d6fe8e01"/>
    Informational = 0x04,
    /// <summary> The LE Controller commands provide access and control to various capabilities of the Bluetooth hardware, as well as methods for the Host to affect how the Link Layer manages the piconet and controls connections </summary>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-bdfa3690-bc8e-efe7-fb5e-0e541199a39b"/>
    LeController = 0x08,
}