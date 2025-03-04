using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.Versioning;

namespace Darp.Ble.HciHost.Usb;

internal static class UsbPortLinux
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
        string strOutput = Call_lsof(portName);
        return !string.IsNullOrEmpty(strOutput);
    }

    private static string Call_lsof(string strDeviceName)
    {
        // Outputs PID of process having opened the device
        //
        using var process = new Process();
        process.StartInfo.FileName = "lsof";
        process.StartInfo.ArgumentList.Add("-t");
        process.StartInfo.ArgumentList.Add("-S2");
        process.StartInfo.ArgumentList.Add("-O");
        process.StartInfo.ArgumentList.Add(strDeviceName);
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();
        string strOutput = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return strOutput;
    }

    private sealed class DeviceProperties
    {
        private const string PROPERTY_VendorId = "ID_VENDOR_ID";
        private const string PROPERTY_Vendor = "ID_VENDOR";
        private const string PROPERTY_ModelId = "ID_MODEL_ID";
        private const string PROPERTY_Model = "ID_MODEL";
        private const string PROPERTY_Type = "ID_TYPE";

        private static readonly string[] PROPERTIES_All =
        [
            PROPERTY_VendorId,
            PROPERTY_Vendor,
            PROPERTY_ModelId,
            PROPERTY_Model,
            PROPERTY_Type,
        ];

        public string? VendorId { get; }
        public string? Vendor { get; }
        public string? ModelId { get; }
        public string? Model { get; }
        public string? Type { get; }

        public DeviceProperties(string strDeviceName)
        {
            string strOutput = Call_udevadm(strDeviceName);

            VendorId = GetPropertyValue(PROPERTY_VendorId, strOutput);
            Vendor = GetPropertyValue(PROPERTY_Vendor, strOutput);
            ModelId = GetPropertyValue(PROPERTY_ModelId, strOutput);
            Model = GetPropertyValue(PROPERTY_Model, strOutput);
            Type = GetPropertyValue(PROPERTY_Type, strOutput);
        }

        private static string Call_udevadm(string strDeviceName)
        {
            // Example for output:
            //
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
            process.StartInfo.ArgumentList.Add("--property=" + string.Join(',', PROPERTIES_All));
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string strOutput = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return strOutput;
        }

        private static string? GetPropertyValue(string strProperty, string str)
        {
            int iStartKey = str.IndexOf(strProperty, StringComparison.Ordinal);
            if (iStartKey < 0)
                return null;

            int iEqual = iStartKey + strProperty.Length;
            if (str[iEqual] != '=')
                return null;

            int iStartValue = iEqual + 1;
            int iEndValue = str.IndexOf('\n', iStartValue);
            if (iEndValue < 0)
                return null;

            return str[iStartValue..iEndValue];
        }
    }
}