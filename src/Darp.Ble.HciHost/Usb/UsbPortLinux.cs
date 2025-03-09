using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace Darp.Ble.HciHost.Usb;

internal static partial class UsbPortLinux
{
    [SupportedOSPlatform("linux")]
    public static IEnumerable<UsbPortInfo> GetPortInfos()
    {
        foreach (string strPortName in SerialPort.GetPortNames())
        {
            DeviceProperties properties;
            ushort nVendorId;
            ushort nProductId;
            ulong nId;
            try
            {
                properties = new DeviceProperties(strPortName);

                nVendorId = Convert.ToUInt16(properties.VendorId, 16);
                nProductId = Convert.ToUInt16(properties.ModelId, 16);

                nId = (ulong)HashCode.Combine(strPortName, properties.VendorId, properties.ModelId) << 32
                    | (uint)HashCode.Combine(properties.Vendor, properties.Model, properties.Type);
            }
            catch
            {
                continue;
            }

            yield return new UsbPortInfo
            {
                Id = nId,
                Type = properties.Type ?? "N/A",
                VendorId = nVendorId,
                ProductId = nProductId,
                Port = strPortName,
                Manufacturer = properties.Vendor,
                Description = properties.Model,
            };
        }
    }

    [SupportedOSPlatform("linux")]
    public static bool IsOpen(string portName)
    {
        string strOutput = CallLsof(portName);
        return GetLsofResultRegex().IsMatch(strOutput);
    }

    private static string CallLsof(string strDeviceName)
    {
        // If device is opened by a process, lsof outputs the PID of that process.
        //
        using var process = new Process();
        process.StartInfo.FileName = "lsof";
        process.StartInfo.ArgumentList.Add("-t");
        process.StartInfo.ArgumentList.Add("-S2");
        process.StartInfo.ArgumentList.Add("-O");
        process.StartInfo.ArgumentList.Add(strDeviceName);
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.Start();
        string strOutput = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return strOutput;
    }

    private sealed class DeviceProperties
    {
        private const string PropertyVendorId = "ID_VENDOR_ID";
        private const string PropertyVendor = "ID_VENDOR";
        private const string PropertyModelId = "ID_MODEL_ID";
        private const string PropertyModel = "ID_MODEL";
        private const string PropertyType = "ID_TYPE";
        private static readonly string[] PropertiesAll =
        [
            PropertyVendorId,
            PropertyVendor,
            PropertyModelId,
            PropertyModel,
            PropertyType,
        ];

        public string? VendorId { get; }
        public string? Vendor { get; }
        public string? ModelId { get; }
        public string? Model { get; }
        public string? Type { get; }

        public DeviceProperties(string strDeviceName)
        {
            string strOutput = CallUdevAdm(strDeviceName);

            VendorId = GetPropertyValue(PropertyVendorId, strOutput);
            Vendor = GetPropertyValue(PropertyVendor, strOutput);
            ModelId = GetPropertyValue(PropertyModelId, strOutput);
            Model = GetPropertyValue(PropertyModel, strOutput);
            Type = GetPropertyValue(PropertyType, strOutput);
        }

        private static string CallUdevAdm(string strDeviceName)
        {
            // Output looks like:
            // ID_MODEL=HCI_via_H4_UART_dongle
            // ID_MODEL_ID=0004
            // ID_VENDOR=ZEPHYR
            // ID_VENDOR_ID=2fe3
            // ID_TYPE=generic

            using var process = new Process();
            process.StartInfo.FileName = "udevadm";
            process.StartInfo.ArgumentList.Add("info");
            process.StartInfo.ArgumentList.Add(strDeviceName);
            process.StartInfo.ArgumentList.Add("--query=property");
            process.StartInfo.ArgumentList.Add("--property=" + string.Join(',', PropertiesAll));
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            string strOutput = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return strOutput;
        }

        private static string? GetPropertyValue(string strProperty, string str)
        {
            var regex = new Regex("^" + strProperty + "=(?<value>.+)$", RegexOptions.Multiline, TimeSpan.FromMilliseconds(100));
            Match m = regex.Match(str);
            return m.Success ? m.Groups["value"].Value : null;
        }
    }

    [GeneratedRegex("^\\d+$", RegexOptions.Multiline, matchTimeoutMilliseconds: 100)]
    private static partial Regex GetLsofResultRegex();
}