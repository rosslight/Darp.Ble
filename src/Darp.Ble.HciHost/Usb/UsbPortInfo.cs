namespace Darp.Ble.HciHost.Usb;

public readonly record struct UsbPortInfo(
    ulong Id,
    string Type,
    ushort VendorId,
    ushort ProductId,
    string? Port,
    string? Manufacturer,
    string? Description
);
