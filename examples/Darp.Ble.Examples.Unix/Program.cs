using System.Globalization;
using Darp.Ble.Gap;
using Darp.Ble.Mock;
using Serilog;
using Serilog.Extensions.Logging;

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

        var extensionsLogger = new SerilogLoggerFactory(Log.Logger);

        using var ble = new Ble();

        BleManager manager = new BleManagerBuilder()
            .AddMock(factory => factory.AddPeripheral(ble.Initialize))
            .SetLogger(extensionsLogger)
            .CreateManager();

        IBleDevice adapter = manager.EnumerateDevices().First();

        _ = ble.StartScanAsync(adapter, OnNextAdvertisement);
        Task.Delay(5000).Wait();
        ble.StopScan();
    }

    private static void OnNextAdvertisement(IGapAdvertisement advertisement)
    {
        Log.Information(
            string.Format(
                CultureInfo.InvariantCulture,
                "Addr=0x{0}, PowerLevel={1}, Rssi={2}, Data=0x{3}",
                advertisement.Address,
                advertisement.TxPower,
                advertisement.Rssi,
                Convert.ToHexString(advertisement.Data.ToByteArray())
            )
        );
    }
}
