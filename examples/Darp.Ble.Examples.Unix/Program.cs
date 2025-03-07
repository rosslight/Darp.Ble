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
            .Add(new HciHostBleFactory())
            .Add(new BleMockFactory { OnInitialize = ble.Initialize })
            .SetLogger(extensionsLogger)
            .CreateManager();

        IBleDevice adapter = manager.EnumerateDevices().First();

        using var test = new Test_IsOpen("/dev/ttyACM0");

        _ = ble.StartScanAsync(adapter, OnNextAdvertisement);
        Task.Delay(15000).Wait();
        ble.StopScan();

        test.Stop();
    }

    private static void OnNextAdvertisement(IGapAdvertisement advertisement)
    {
        Log.Information("Addr=0x{0}, PowerLevel={1}, Rssi={2}, Data=0x{3}",
            advertisement.Address,
            advertisement.TxPower,
            advertisement.Rssi,
            Convert.ToHexString(advertisement.Data.ToByteArray()));
    }

    private sealed class Test_IsOpen : IDisposable
    {
        private readonly CancellationTokenSource m_cancelSource = new();

        public Test_IsOpen(string strPortName)
        {
            CancellationToken cancelTok = m_cancelSource.Token;

            _ = Task.Run(async () =>
            {
                while (!cancelTok.IsCancellationRequested)
                {
                    Console.WriteLine("IsOpen({0})={1}", strPortName, HciHost.Usb.UsbPort.IsOpen(strPortName));
                    await Task.Delay(200);
                }
            }, cancelTok);

            Task.Delay(3000).Wait();
        }

        public void Stop()
        {
            Task.Delay(3000).Wait();

            m_cancelSource.Cancel();
        }

        public void Dispose()
        {
            m_cancelSource.Dispose();
        }
    }
}
