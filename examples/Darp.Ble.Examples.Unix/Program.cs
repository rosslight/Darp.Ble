// See https://aka.ms/new-console-template for more information

using System.Globalization;
using Darp.Ble.Examples.Unix.Mockup;
using Serilog;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Darp.Ble.Examples.Unix;

sealed class Program
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
    }
}
