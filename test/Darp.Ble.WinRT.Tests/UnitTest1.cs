using System.Globalization;
using System.Reactive.Linq;
using Serilog;
using Serilog.Events;
using Xunit.Abstractions;

namespace Darp.Ble.WinRT.Tests;

public sealed class UnitTest1(ITestOutputHelper outputHelper)
{
    private readonly ILogger _logger = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .WriteTo.TestOutput(outputHelper, formatProvider:CultureInfo.InvariantCulture)
        .CreateLogger();

    //[Fact]
    public async Task Test1()
    {
        BleManager manager = new BleManagerBuilder()
            .OnLog((_, logEvent) => _logger.Write((LogEventLevel)logEvent.Level, logEvent.Exception, logEvent.MessageTemplate, logEvent.Properties))
            .With<WinBleFactory>()
            .CreateManager();
        BleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync();
        await device.Observer
            .RefCount()
            .Take(100);
        await Task.Delay(1000);
    }
}