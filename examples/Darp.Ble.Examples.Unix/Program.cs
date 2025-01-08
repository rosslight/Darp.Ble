using System.Globalization;
using Darp.Ble.Data;
using Darp.Ble.Examples.Unix.Mockup;
using Darp.Ble.Gap;
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

        BleManager manager = new BleManagerBuilder()
            .Add<BMFactory>()
            .SetLogger(extensionsLogger)
            .CreateManager();

        IBleDevice adapter = manager.EnumerateDevices().First();

        var ble = new Ble();
        _ = ble.StartScanAsync(adapter, OnAdvertisement);
        Task.Delay(10000).Wait();
        ble.StopScan();
    }

    private static void OnAdvertisement(IGapAdvertisement advertisement)
    {
        Log.Information(advertisement.Address.ToString());
    }

    private sealed class Ble
    {
        private IBleObserver? m_observer;
        private IDisposable? m_unsubscriber;

        public async Task StartScanAsync(IBleDevice adapter, Action<IGapAdvertisement> onNextAdvertisement)
        {
            StopScan();

            await adapter.InitializeAsync();
            m_observer = adapter.Observer;

            m_observer.Configure(new BleScanParameters()
            {
                ScanType = ScanType.Active,
                ScanWindow = ScanTiming.Ms100,
                ScanInterval = ScanTiming.Ms100,
            });

            m_unsubscriber = m_observer.Subscribe(onNextAdvertisement);

            m_observer.Connect();
        }

        public void StopScan()
        {
            m_unsubscriber?.Dispose();
            m_unsubscriber = null;

            m_observer?.StopScan();
            m_observer = null;
        }
    }
}
