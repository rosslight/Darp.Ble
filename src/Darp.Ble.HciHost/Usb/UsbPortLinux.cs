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
            string? strVendorId;
            string? strVendor;
            string? strProductId;
            string? strType;
            string? strDescription;
            ushort nVendorId;
            ushort nProductId;
            try
            {
                strVendorId = UDevAdmin.GetProperty(strPortName, UDevAdmin.PROPERTY_VendorId);
                strVendor = UDevAdmin.GetProperty(strPortName, UDevAdmin.PROPERTY_Vendor);
                strProductId = UDevAdmin.GetProperty(strPortName, UDevAdmin.PROPERTY_ModelId);
                strDescription = UDevAdmin.GetProperty(strPortName, UDevAdmin.PROPERTY_Model);
                strType = UDevAdmin.GetProperty(strPortName, UDevAdmin.PROPERTY_Type);

                nVendorId = Convert.ToUInt16(strVendorId, 16);
                nProductId = Convert.ToUInt16(strProductId, 16);
            }
            catch
            {
                continue;
            }

            yield return new UsbPortInfo
            {
                Id = 0, //TODO
                Type = strType ?? "N/A",
                VendorId = nVendorId,
                ProductId = nProductId,
                Port = strPortName,
                Manufacturer = strVendor,
                Description = strDescription,
            };
        }
    }

    [SupportedOSPlatform("linux")]
    public static bool IsOpen(string portName)
    {
        throw new NotSupportedException();
    }

    private static class UDevAdmin
    {
        public const string PROPERTY_VendorId = "ID_VENDOR_ID";
        public const string PROPERTY_Vendor = "ID_VENDOR";
        public const string PROPERTY_ModelId = "ID_MODEL_ID";
        public const string PROPERTY_Model = "ID_MODEL";
        public const string PROPERTY_Type = "ID_TYPE";

        public static string? GetProperty(string strDeviceName, string strProperty)
        {
            using var process = new Process();
            process.StartInfo.FileName = "udevadm";
            process.StartInfo.ArgumentList.Add("info");
            process.StartInfo.ArgumentList.Add(strDeviceName);
            process.StartInfo.ArgumentList.Add("--query=property");
            process.StartInfo.ArgumentList.Add("--property=" + strProperty);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            string strOutput = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            string strPrefix = strProperty + "=";
            if (strOutput.StartsWith(strPrefix, StringComparison.Ordinal))
                return strOutput[strPrefix.Length..].TrimEnd();

            return null;
        }
    }
}