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
            UDevAdmin.Properties? properties;
            ushort nVendorId;
            ushort nProductId;
            try
            {
                properties = UDevAdmin.GetProperties(strPortName);

                nVendorId = Convert.ToUInt16(properties?.VendorId, 16);
                nProductId = Convert.ToUInt16(properties?.ModelId, 16);
            }
            catch
            {
                continue;
            }

            yield return new UsbPortInfo
            {
                Id = 0, //TODO
                Type = properties?.Type ?? "N/A",
                VendorId = nVendorId,
                ProductId = nProductId,
                Port = strPortName,
                Manufacturer = properties?.Vendor,
                Description = properties?.Model,
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
        private const string PROPERTY_VendorId = "ID_VENDOR_ID";
        private const string PROPERTY_Vendor = "ID_VENDOR";
        private const string PROPERTY_ModelId = "ID_MODEL_ID";
        private const string PROPERTY_Model = "ID_MODEL";
        private const string PROPERTY_Type = "ID_TYPE";

        private static readonly string[] PropertiesAll =
        [
            PROPERTY_VendorId,
            PROPERTY_Vendor,
            PROPERTY_ModelId,
            PROPERTY_Model,
            PROPERTY_Type,
        ];

        public record Properties(
            string? VendorId,
            string? Vendor,
            string? ModelId,
            string? Model,
            string? Type
        );

        public static Properties? GetProperties(string strDeviceName)
        {
            using var process = new Process();
            process.StartInfo.FileName = "udevadm";
            process.StartInfo.ArgumentList.Add("info");
            process.StartInfo.ArgumentList.Add(strDeviceName);
            process.StartInfo.ArgumentList.Add("--query=property");
            process.StartInfo.ArgumentList.Add("--property=" + string.Join(',', PropertiesAll));
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            string strOutput = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            return new Properties(
                VendorId: GetValueFor(PROPERTY_VendorId, strOutput),
                Vendor: GetValueFor(PROPERTY_Vendor, strOutput),
                ModelId: GetValueFor(PROPERTY_ModelId, strOutput),
                Model: GetValueFor(PROPERTY_Model, strOutput),
                Type: GetValueFor(PROPERTY_Type, strOutput));
        }

        private static string? GetValueFor(string strProperty, string str)
        {
            int iStartKey = str.IndexOf(strProperty, StringComparison.Ordinal);
            if (iStartKey < 0)
                return null;

            int iEqualSign = iStartKey + strProperty.Length;
            if (str[iEqualSign] != '=')
                return null;

            int iStartValue = iEqualSign + 1;
            int iEndValue = str.IndexOf('\n', iStartValue);
            if (iEndValue < 0)
                return null;

            return str[iStartValue..iEndValue];
        }
    }
}