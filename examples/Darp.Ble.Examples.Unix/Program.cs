using System.Globalization;
using Darp.Ble.Gap;
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
            .MinimumLevel.Verbose()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        ILogger extensionsLogger = new SerilogLoggerFactory(Log.Logger).CreateLogger("Ble");

        using var ble = new Ble();

        BleManager manager = new BleManagerBuilder()
            .Add(new BleMockFactory { OnInitialize = ble.Initialize })
            .SetLogger(extensionsLogger)
            .CreateManager();

        IBleDevice adapter = manager.EnumerateDevices().First();

        _ = ble.StartScanAsync(adapter, OnNextAdvertisement);
        Task.Delay(5000).Wait();
        ble.StopScan();
    }

    private static void OnNextAdvertisement(IGapAdvertisement advertisement)
    {
        Log.Information(string.Format(CultureInfo.InvariantCulture, "Addr={0}, Data=0x{1}",
            advertisement.Address,
            Convert.ToHexString(advertisement.Data.ToByteArray())));
    }
}
