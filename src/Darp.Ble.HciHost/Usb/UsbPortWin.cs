using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;

namespace Darp.Ble.HciHost.Usb;

/// <summary> Usb port utilities for windows </summary>
internal static partial class UsbPortWin
{
    private const int ReadPropertyBufferSize = 256;

    // Flags for SetupDiGetClassDevs
    private const uint DigcfPresent = 0x00000002;

    // Property to retrieve the device description
    private const uint SpdrpDeviceDesc = 0x00000000;
    private const uint SpdrpMfg = 0x0000000B;
    private const uint SpdrpDevType = 0x00000010;

    // Constants for SetupDiOpenDevRegKey
    private const uint DicsFlagGlobal = 0x00000001;
    private const uint DiregDev = 0x00000001;
    private const uint KeyRead = 0x20019; // Standard KEY_READ access

    // Ports (COM & LPT-Ports)
    // See https://learn.microsoft.com/en-us/windows-hardware/drivers/install/system-defined-device-setup-classes-available-to-vendors
    private static readonly Guid GuidPorts = new("4d36e978-e325-11ce-bfc1-08002be10318");

    /// <summary> Enumerate all devices that are available. Only finds USB devices that are part of the "Ports (COM and LPT ports)" devices </summary>
    /// <returns> USB Port infos </returns>
    /// <seealso href="https://cdn.velleman.eu/images/tmp/usbfind.c"/>
    /// <seealso href="https://gist.github.com/cobrce/70ecfe16f0e88bc9f7da7d970556352f"/>
    [SupportedOSPlatform("windows")]
    public static IEnumerable<UsbPortInfo> GetPortInfos()
    {
        // Get a handle to the device information set for all present USB devices.
        IntPtr deviceInfoSet = SetupDiGetClassDevsW(in GuidPorts, IntPtr.Zero, IntPtr.Zero, DigcfPresent);
        if (deviceInfoSet == IntPtr.Zero)
            yield break;

        try
        {
            SpDevInfoData deviceInfoData = new();
            var deviceIndex = 0;

            while (SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData))
            {
                deviceIndex++;

                IntPtr hDevRegKey = SetupDiOpenDevRegKey(
                    deviceInfoSet,
                    deviceInfoData,
                    DicsFlagGlobal,
                    0,
                    DiregDev,
                    KeyRead
                );
                if (hDevRegKey == new IntPtr(-1))
                    continue;
                string portName;
                try
                {
                    if (!TryGetRegistryStringValue(hDevRegKey, "PortName", out portName))
                        continue;
                }
                finally
                {
                    RegCloseKey(hDevRegKey);
                }

                if (!deviceInfoData.TryGetDeviceInstanceId(deviceInfoSet, out string? deviceHardwareId))
                    continue;
                Match idMatch = GetDeviceHardwareIdsRegex().Match(deviceHardwareId);
                if (!idMatch.Success)
                    continue;
                var vendorId = Convert.ToUInt16(idMatch.Groups["VendorId"].Value, 16);
                var productId = Convert.ToUInt16(idMatch.Groups["ProductId"].Value, 16);
                deviceInfoData.TryGetDeviceProperty(deviceInfoSet, SpdrpDeviceDesc, out string? deviceDescription);
                deviceInfoData.TryGetDeviceProperty(deviceInfoSet, SpdrpDevType, out string? deviceType);
                deviceInfoData.TryGetDeviceProperty(deviceInfoSet, SpdrpMfg, out string? manufacturerName);

                yield return new UsbPortInfo
                {
                    Id = (uint)deviceHardwareId.GetHashCode(StringComparison.InvariantCulture),
                    Type = deviceType ?? "n/a",
                    VendorId = vendorId,
                    ProductId = productId,
                    Port = portName,
                    Manufacturer = manufacturerName,
                    Description = deviceDescription,
                };
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool TryGetDeviceProperty(
        this in SpDevInfoData deviceInfoData,
        IntPtr deviceInfoSet,
        uint propertyIdentifier,
        [NotNullWhen(true)] out string? deviceProperty
    )
    {
        Span<char> deviceNameBuffer = stackalloc char[ReadPropertyBufferSize];
        if (
            SetupDiGetDeviceRegistryPropertyW(
                deviceInfoSet,
                in deviceInfoData,
                propertyIdentifier,
                out uint _,
                deviceNameBuffer,
                deviceNameBuffer.Length,
                out int _
            )
        )
        {
            deviceProperty = deviceNameBuffer.ToStringNullTerminated();
            return true;
        }

        deviceProperty = null;
        return false;
    }

    [SupportedOSPlatform("windows")]
    private static bool TryGetRegistryStringValue(IntPtr hKey, ReadOnlySpan<char> valueName, out string entry)
    {
        Span<char> entryBuffer = stackalloc char[ReadPropertyBufferSize];
        var dataSize = (uint)entryBuffer.Length;
        int ret = RegQueryValueExW(hKey, valueName, 0, out _, entryBuffer, ref dataSize);
        //int ret = DllImports.RegQueryValueExW(hKey, valueName, 0, out _, entryBuffer, ref dataSize);
        if (ret != 0)
        {
            entry = string.Empty;
            return false;
        }

        entry = entryBuffer.ToStringNullTerminated();
        return true;
    }

    [SupportedOSPlatform("windows")]
    private static bool TryGetDeviceInstanceId(
        this in SpDevInfoData deviceInfoData,
        IntPtr deviceInfoSet,
        [NotNullWhen(true)] out string? deviceInstance
    )
    {
        Span<char> entryBuffer = stackalloc char[ReadPropertyBufferSize];
        bool ret = SetupDiGetDeviceInstanceIdW(
            deviceInfoSet,
            in deviceInfoData,
            entryBuffer,
            entryBuffer.Length,
            out _
        );
        if (!ret)
        {
            deviceInstance = null;
            return false;
        }

        deviceInstance = entryBuffer.ToStringNullTerminated();
        return true;
    }

    [SupportedOSPlatform("windows")]
    public static bool IsOpen(string portName)
    {
        const int dwFlagsAndAttributes = 0x40000000;
        using SafeFileHandle hFile = CreateFileW(
            @"\\.\" + portName,
            -1073741824,
            0,
            IntPtr.Zero,
            3,
            dwFlagsAndAttributes,
            IntPtr.Zero
        );
        if (hFile.IsInvalid)
            return true;
        hFile.Close();
        return false;
    }

    private static string ToStringNullTerminated(this in Span<char> source)
    {
        int nullTermination = source.IndexOf('\0');
        return nullTermination < 0 ? source.ToString() : source[..nullTermination].ToString();
    }

    [GeneratedRegex(
        "VID_(?<VendorId>[0-9A-F]{4})&PID_(?<ProductId>[0-9A-F]{4})",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 100
    )]
    private static partial Regex GetDeviceHardwareIdsRegex();

    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew" />
    [SupportedOSPlatform("windows")]
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    private static partial SafeFileHandle CreateFileW(
        string lpFileName,
        int dwDesiredAccess,
        int dwShareMode,
        IntPtr securityAttrs,
        int dwCreationDisposition,
        int dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );

    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdigetclassdevsw" />
    [SupportedOSPlatform("windows")]
    [LibraryImport("setupapi.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    private static partial IntPtr SetupDiGetClassDevsW(
        in Guid classGuid,
        IntPtr enumerator,
        IntPtr hwndParent,
        uint flags
    );

    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdienumdeviceinfo" />
    [SupportedOSPlatform("windows")]
    [LibraryImport("setupapi.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetupDiEnumDeviceInfo(
        IntPtr deviceInfoSet,
        int memberIndex,
        ref SpDevInfoData deviceInfoData
    );

    [LibraryImport("setupapi.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetupDiGetDeviceInstanceIdW(
        IntPtr deviceInfoSet,
        in SpDevInfoData deviceInfoData,
        Span<char> deviceInstanceId,
        int deviceInstanceIdSize,
        out int requiredSize
    );

    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdigetdeviceregistrypropertyw" />
    [SupportedOSPlatform("windows")]
    [LibraryImport("setupapi.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetupDiGetDeviceRegistryPropertyW(
        IntPtr deviceInfoSet,
        in SpDevInfoData deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        Span<char> propertyBuffer,
        int propertyBufferSize,
        out int requiredSize
    );

    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdiopendevregkey" />
    [LibraryImport("setupapi.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    private static partial IntPtr SetupDiOpenDevRegKey(
        IntPtr deviceInfoSet,
        in SpDevInfoData deviceInfoData,
        uint scope,
        uint hwProfile,
        uint keyType,
        uint samDesired
    );

    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdidestroydeviceinfolist" />
    [SupportedOSPlatform("windows")]
    [LibraryImport("setupapi.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct SpDevInfoData()
    {
        public readonly int cbSize = Marshal.SizeOf<SpDevInfoData>();
        public readonly Guid ClassGuid;
        public readonly int DevInst;
        public readonly IntPtr Reserved;
    }

    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-RegQueryValueEx" />
    [LibraryImport("advapi32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    private static partial int RegQueryValueExW(
        IntPtr hKey,
        ReadOnlySpan<char> lpValueName,
        int lpReserved,
        out uint lpType,
        Span<char> lpData,
        ref uint lpcbData
    );

    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regclosekey" />
    [LibraryImport("advapi32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
    private static partial int RegCloseKey(IntPtr hKey);
}
