using System.Globalization;
using Darp.Ble.Gap;
using Darp.Ble.HciHost;
using Darp.Ble.HciHost.Usb;
using Darp.Ble.Mock;
using Serilog;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Darp.Ble.Examples.Unix;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        ILogger extensionsLogger = new SerilogLoggerFactory(Log.Logger).CreateLogger("Ble");

        using var ble = new Ble();

        BleManager manager = new BleManagerBuilder()
            .Add(new HciHostBleFactory())
            .Add(new BleMockFactory { OnInitialize = ble.Initialize })
            .SetLogger(extensionsLogger)
            .CreateManager();

        IBleDevice adapter = manager.EnumerateDevices().First();

        // _ = Task.Run(async () =>
        // {
        //     for (; ; )
        //     {
        //         Console.WriteLine(UsbPort.IsOpen("/dev/ttyACM0"));
        //         await Task.Delay(100);
        //     }
        // });

        // Task.Delay(3000).Wait();

        _ = ble.StartScanAsync(adapter, OnNextAdvertisement);
        Task.Delay(15000).Wait();
        ble.StopScan();

        // Task.Delay(3000).Wait();
    }

    private static void OnNextAdvertisement(IGapAdvertisement advertisement)
    {
        Log.Information(string.Format(CultureInfo.InvariantCulture, "Addr=0x{0}, PowerLevel={1}, Rssi={2}, Data=0x{3}",
            advertisement.Address,
            advertisement.TxPower,
            advertisement.Rssi,
            Convert.ToHexString(advertisement.Data.ToByteArray())));
    }
}
