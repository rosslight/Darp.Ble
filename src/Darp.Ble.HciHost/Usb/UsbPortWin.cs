using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;

namespace Darp.Ble.HciHost.Usb;

internal static partial class UsbPortWin
{
    [SupportedOSPlatform("windows")]
    public static IEnumerable<UsbPortInfo> GetPortInfos()
    {
        using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"");
        foreach (ManagementBaseObject p in searcher.Get().Cast<ManagementBaseObject>())
        {
            Match portMatch = GetNamePortRegex().Match((string)p["Name"]);

            var pnpDeviceId = (string)p["PNPDeviceID"];
            // USB\VID_1915&PID_C00A&MI_01\6&3116A719&0&0001
            Match typeMatch = GetDeviceTypeRegex().Match(pnpDeviceId); // USB
            Match vendorMatch = GetDeviceVendorIdRegex().Match(pnpDeviceId); // 1915
            Match productMatch = GetDeviceProductIdRegex().Match(pnpDeviceId); // C00A
            Match parentIdPrefixMatch = GetDeviceParentIdPrefixRegex().Match(pnpDeviceId); // 6&3116A719&0
            if (!typeMatch.Success
                || !vendorMatch.Success
                || !productMatch.Success
                || !parentIdPrefixMatch.Success)
            {
                continue;
            }

            ushort vendorId;
            ushort productId;
            ulong id;
            try
            {
                vendorId = Convert.ToUInt16(vendorMatch.Groups["VendorId"].Value, 16);
                productId = Convert.ToUInt16(productMatch.Groups["ProductId"].Value, 16);
                id = GetHashCodeUInt64(parentIdPrefixMatch.Groups["ParentIdPrefix"].Value);
            }
            catch (Exception)
            {
                continue;
            }

            yield return new UsbPortInfo
            {
                Id = id,
                Type = typeMatch.Groups["DeviceType"].Value,
                VendorId = vendorId,
                ProductId = productId,
                Port = portMatch.Success ? portMatch.Groups["Port"].Value : null,
                Manufacturer = (string?)p["Manufacturer"],
                Description = (string?)p["Caption"],
            };
        }
    }

    private static ulong GetHashCodeUInt64(string input)
    {
        string s1 = input[..(input.Length / 2)];
        string s2 = input[(input.Length / 2)..];

        ulong x= (ulong)StringComparer.Ordinal.GetHashCode(s1) << 0x20 | (uint)StringComparer.Ordinal.GetHashCode(s2);

        return x;
    }

    [SupportedOSPlatform("windows")]
    public static bool IsOpen(string portName)
    {
        const int dwFlagsAndAttributes = 0x40000000;
        using SafeFileHandle hFile = CreateFile(@"\\.\" + portName,
            -1073741824,
            0,
            IntPtr.Zero,
            3,
            dwFlagsAndAttributes,
            IntPtr.Zero);
        if (hFile.IsInvalid)
            return true;
        hFile.Close();
        return false;
    }

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, int dwShareMode, IntPtr securityAttrs, int dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

    [GeneratedRegex("VID_(?<VendorId>[0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 100)]
    private static partial Regex GetDeviceVendorIdRegex();
    [GeneratedRegex(@"PID_(?<ProductId>[0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 100)]
    private static partial Regex GetDeviceProductIdRegex();
    [GeneratedRegex(@"\\(?<ParentIdPrefix>[^\\]*)&[\d\w]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 100)]
    private static partial Regex GetDeviceParentIdPrefixRegex();
    [GeneratedRegex(@"^(?<DeviceType>[\w\d]+)\\", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 100)]
    private static partial Regex GetDeviceTypeRegex();
    [GeneratedRegex(@"\((?<Port>[\w\d]*)\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 100)]
    private static partial Regex GetNamePortRegex();
}