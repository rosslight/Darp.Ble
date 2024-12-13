// See https://aka.ms/new-console-template for more information

using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using Darp.Ble.HciHost;
using Darp.Ble.WinRT;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

Logger logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

ILogger extensionsLogger = new SerilogLoggerFactory(logger).CreateLogger("Ble");

BleManager manager = new BleManagerBuilder()
    .Add<WinBleFactory>()
    .SetLogger(extensionsLogger)
    .CreateManager();

IBleDevice x = manager.EnumerateDevices().First();
await x.InitializeAsync();
var counter = 0;
for (int ii = 0; ii < 1000; ii++)
{
    try
    {
        var peer1 = await ConnectToDevice(x);
        await peer1.DisposeAsync();
        Console.WriteLine($"Asd {ii}");
    }
    catch (Exception)
    {
        counter++;
    }
}
int i = 0;

async Task<IGattServerPeer> ConnectToDevice(IBleDevice device)
{
    var advertisement = await device.Observer.RefCount()
        .Where(x => (x.EventType & BleEventType.Connectable) != 0)
        .Where(a =>
    {
        return a.Data.TryGetManufacturerSpecificData(out var dataTuple) &&
               dataTuple.Company is CompanyIdentifiers.AdolfWuerthGmbHCoKg;
    }).FirstAsync();
    return await advertisement.Connect().FirstAsync();
}

async Task Observe(IBleDevice device)
{
    var advertisement = await device.Observer.RefCount()
        .Where(x => (x.EventType & BleEventType.Connectable) != 0)
        .Where(a =>
        {
            return a.Data.TryGetManufacturerSpecificData(out var dataTuple) &&
                   dataTuple.Company is CompanyIdentifiers.Abbott;
        }).FirstAsync();
    await using var peer = await advertisement.Connect().FirstAsync();
    var service = await peer.DiscoverServiceAsync(new BleUuid(0x1234));
    var writeChar = await service.DiscoverCharacteristicAsync<Properties.Write>(new BleUuid(0x1235));
    var notifyChar = await service.DiscoverCharacteristicAsync<Properties.Notify>(new BleUuid(0x1236));
    await using IAsyncDisposable disposable = await notifyChar.OnNotifyAsync(bytes =>
    {
        // ...
    });
    await using var disposableObs = await notifyChar.OnNotifyAsync();
    var notifyConnected = disposableObs.FirstAsync().ToTask();
    await writeChar.WriteAsync(Convert.FromHexString("0011223344"));
    var resultBytes = await notifyConnected;
    int i = 0;
}

async Task Advertise(IBleDevice device)
{
    IGattClientService service = await device.Peripheral.AddServiceAsync(0x1234);
    IGattClientCharacteristic<Properties.Write> characteristic =
        await service.AddCharacteristicAsync<Properties.Write>(0x1235);
    IGattClientCharacteristic<Properties.Notify> notify =
        await service.AddCharacteristicAsync<Properties.Notify>(0x1236);
    characteristic.OnWrite(async (peer, bytes, token) =>
    {
        await notify.NotifyAsync(peer, bytes, token);
        return GattProtocolStatus.Success;
    });

    x.Broadcaster.Advertise(AdvertisingData.From([
            (AdTypes.CompleteListOf16BitServiceOrServiceClassUuids, [0x34, 0x12]),
            (AdTypes.ManufacturerSpecificData, [0x04, 0x00, 0x01, 0x02, 0x03, 0x04]),
        ]),
        TimeSpan.FromMilliseconds(100), new AdvertisingParameters
        {
            Type = BleEventType.Connectable,
        });
}

await Task.Delay(20000000);