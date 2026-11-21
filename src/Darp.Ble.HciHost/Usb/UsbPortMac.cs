using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Darp.Ble.HciHost.Usb;

[SupportedOSPlatform("macos")]
internal static partial class UsbPortMac
{
    private const string IOKit = "/System/Library/Frameworks/IOKit.framework/IOKit";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    private const uint Utf8 = 0x08000100;
    private const int CfNumberSInt32Type = 3;

    public static IEnumerable<UsbPortInfo> GetPortInfos()
    {
        IntPtr matching = IOServiceMatching("IOSerialBSDClient");
        // IOServiceGetMatchingServices consumes the matching dictionary.
        if (matching == IntPtr.Zero || IOServiceGetMatchingServices(0, matching, out uint iterator) != 0)
            yield break;

        try
        {
            uint service;
            while ((service = IOIteratorNext(iterator)) != 0)
            {
                UsbPortInfo? port;
                try
                {
                    port = GetPortInfo(service);
                }
                finally
                {
                    IOObjectRelease(service);
                }

                if (port is { } info)
                    yield return info;
            }
        }
        finally
        {
            IOObjectRelease(iterator);
        }
    }

    private static UsbPortInfo? GetPortInfo(uint service)
    {
        string? port = ReadString(service, "IOCalloutDevice");
        if (string.IsNullOrEmpty(port))
            return null;

        uint usbDevice = FindUsbDevice(service);
        if (usbDevice == 0)
            return null;

        try
        {
            if (
                ReadUsbId(usbDevice, "idVendor") is not { } vendorId
                || ReadUsbId(usbDevice, "idProduct") is not { } productId
                || IORegistryEntryGetRegistryEntryID(usbDevice, out ulong id) != 0
            )
                return null;

            return new UsbPortInfo(
                id,
                ReadString(service, "IOSerialBSDClientType") ?? "N/A",
                vendorId,
                productId,
                port,
                ReadString(usbDevice, "USB Vendor Name"),
                ReadString(usbDevice, "USB Product Name")
            );
        }
        finally
        {
            IOObjectRelease(usbDevice);
        }
    }

    private static uint FindUsbDevice(uint service)
    {
        uint entry = service;
        while (IORegistryEntryGetParentEntry(entry, "IOService", out uint parent) == 0)
        {
            if (entry != service)
                IOObjectRelease(entry);
            if (IOObjectConformsTo(parent, "IOUSBHostDevice") != 0 || IOObjectConformsTo(parent, "IOUSBDevice") != 0)
                return parent;
            entry = parent;
        }

        if (entry != service)
            IOObjectRelease(entry);
        return 0;
    }

    private static IntPtr CreateProperty(uint entry, string name)
    {
        IntPtr key = CFStringCreateWithCString(IntPtr.Zero, name, Utf8);
        if (key == IntPtr.Zero)
            return IntPtr.Zero;
        try
        {
            return IORegistryEntryCreateCFProperty(entry, key, IntPtr.Zero, 0);
        }
        finally
        {
            CFRelease(key);
        }
    }

    private static ushort? ReadUsbId(uint entry, string name)
    {
        IntPtr value = CreateProperty(entry, name);
        if (value == IntPtr.Zero)
            return null;
        try
        {
            if (
                CFGetTypeID(value) == CFNumberGetTypeID()
                && CFNumberGetValue(value, CfNumberSInt32Type, out int number)
                && number is >= 0 and <= ushort.MaxValue
            )
                return (ushort)number;

            return null;
        }
        finally
        {
            CFRelease(value);
        }
    }

    private static unsafe string? ReadString(uint entry, string name)
    {
        IntPtr value = CreateProperty(entry, name);
        if (value == IntPtr.Zero)
            return null;
        try
        {
            if (CFGetTypeID(value) != CFStringGetTypeID())
                return null;

            nint capacity = CFStringGetMaximumSizeForEncoding(CFStringGetLength(value), Utf8) + 1;
            if (capacity is <= 0 or > int.MaxValue)
                return null;

            var buffer = new byte[(int)capacity];
            fixed (byte* pointer = buffer)
            {
                return CFStringGetCString(value, pointer, capacity, Utf8)
                    ? Marshal.PtrToStringUTF8((IntPtr)pointer)
                    : null;
            }
        }
        finally
        {
            CFRelease(value);
        }
    }

    [LibraryImport(IOKit, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr IOServiceMatching(string name);

    [LibraryImport(IOKit)]
    private static partial int IOServiceGetMatchingServices(uint mainPort, IntPtr matching, out uint iterator);

    [LibraryImport(IOKit)]
    private static partial uint IOIteratorNext(uint iterator);

    [LibraryImport(IOKit)]
    private static partial int IOObjectRelease(uint entry);

    [LibraryImport(IOKit, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int IOObjectConformsTo(uint entry, string className);

    [LibraryImport(IOKit, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int IORegistryEntryGetParentEntry(uint entry, string plane, out uint parent);

    [LibraryImport(IOKit)]
    private static partial int IORegistryEntryGetRegistryEntryID(uint entry, out ulong id);

    [LibraryImport(IOKit)]
    private static partial IntPtr IORegistryEntryCreateCFProperty(uint entry, IntPtr key, IntPtr allocator, uint options);

    [LibraryImport(CoreFoundation, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr CFStringCreateWithCString(IntPtr allocator, string value, uint encoding);

    [LibraryImport(CoreFoundation)]
    private static partial nint CFStringGetLength(IntPtr value);

    [LibraryImport(CoreFoundation)]
    private static partial nint CFStringGetMaximumSizeForEncoding(nint length, uint encoding);

    [LibraryImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static unsafe partial bool CFStringGetCString(IntPtr value, byte* buffer, nint capacity, uint encoding);

    [LibraryImport(CoreFoundation)]
    private static partial nuint CFGetTypeID(IntPtr value);

    [LibraryImport(CoreFoundation)]
    private static partial nuint CFStringGetTypeID();

    [LibraryImport(CoreFoundation)]
    private static partial nuint CFNumberGetTypeID();

    [LibraryImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool CFNumberGetValue(IntPtr value, nint type, out int number);

    [LibraryImport(CoreFoundation)]
    private static partial void CFRelease(IntPtr value);
}
