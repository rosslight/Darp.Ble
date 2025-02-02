using System.Globalization;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Serilog;
using Serilog.Events;
using Xunit.Abstractions;

namespace Darp.Ble.WinRT.Tests;

public sealed class UnitTest1(ITestOutputHelper outputHelper)
{
    private readonly ILogger _logger = new LoggerConfiguration()
        .MinimumLevel.Verbose()
        .WriteTo.TestOutput(outputHelper, formatProvider: CultureInfo.InvariantCulture)
        .CreateLogger();

    //[Fact]
    public async Task Test1()
    {
        BleManager manager = new BleManagerBuilder()
            .OnLog(
                (_, logEvent) =>
                    _logger.Write(
                        (LogEventLevel)logEvent.Level,
                        logEvent.Exception,
                        logEvent.MessageTemplate,
                        logEvent.Properties
                    )
            )
            .With<WinBleFactory>()
            .CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync();
        await device.Observer.RefCount().Take(100);
        await Task.Delay(1000);
    }

    //[Fact]
    public async Task Test2()
    {
        BleManager manager = new BleManagerBuilder()
            .OnLog(
                (_, logEvent) =>
                    _logger.Write(
                        (LogEventLevel)logEvent.Level,
                        logEvent.Exception,
                        logEvent.MessageTemplate,
                        logEvent.Properties
                    )
            )
            .With<WinBleFactory>()
            .CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync();
        var service = await device.Peripheral.AddServiceAsync(new BleUuid(0x1234));
        var characteristic = await service.AddCharacteristicAsync<Properties.Notify>(
            new BleUuid(0x1234)
        );
        await Task.Delay(1000);
    }
}
