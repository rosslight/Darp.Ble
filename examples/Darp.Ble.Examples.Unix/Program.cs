using System.Globalization;
using Darp.Ble.Gap;
using Darp.Ble.HciHost;
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
            .Add(new BleMockFactory { OnInitialize = ble.Initialize })
            .Add(new HciHostBleFactory())
            .SetLogger(extensionsLogger)
            .CreateManager();

        // "Darp.Ble.Mock"
        IBleDevice adapter = manager.EnumerateDevices().First(x => string.Equals(x.Identifier, "Darp.Ble.HciHost", StringComparison.Ordinal));

        _ = ble.StartScanAsync(adapter, OnNextAdvertisement);
        Task.Delay(15000).Wait();
        ble.StopScan();
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
