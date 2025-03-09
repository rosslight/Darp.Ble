namespace Darp.Ble.HciHost.Usb;

/// <summary> A struct providing info about a USB port </summary>
/// <param name="Id"> The USB device's parent hub or controller ID </param>
/// <param name="Type"> The type of the USB device </param>
/// <param name="VendorId"> The VendorId of the USB device </param>
/// <param name="ProductId"> The ProductId of the USB device </param>
/// <param name="Port"> The PortName of the device </param>
/// <param name="Manufacturer"> The name of the manufacturer </param>
/// <param name="Description"> An optional description </param>
public readonly record struct UsbPortInfo(ulong Id,
    string Type,
    ushort VendorId,
    ushort ProductId,
    string? Port,
    string? Manufacturer,
    string? Description);
