using System.Diagnostics;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Gatt.Services;
using Darp.Ble.Hci;
using Darp.Ble.HciHost;
using Darp.Ble.Mock;
using Darp.Ble.WinRT;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates.Themes;
using SerilogTracing;
using SerilogTracing.Expressions;

await using Logger logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(
        formatter: Formatters.CreateConsoleTextFormatter(TemplateTheme.Code)
    //        outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}",
    //        formatProvider: CultureInfo.InvariantCulture
    )
    .WriteTo.Seq("http://localhost:5341", formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

var d = new ServiceCollection()
    .AddLogging(builder => builder.AddSerilog(logger))
    .AddBleManager(builder => builder.AddWinRT())
    .BuildServiceProvider()
    .GetRequiredService<BleManager>()
    .EnumerateDevices()
    .First();
await d.InitializeAsync();

var myObservable = d.Observer.Publish().RefCount();

for (var ii = 0; ii < 10; ii++)
{
    var resultTask = X(myObservable.WhereConnectable()).FirstAsync().ToTask();
    var peer = await resultTask;
    logger.Information("Woo {@Peer}", peer);
    await peer.DisposeAsync();
}

static IObservable<IGattServerPeer> X(IObservable<IGapAdvertisement> source)
{
    return Observable.Create<IGattServerPeer>(observer =>
    {
        return source
            .SelectMany(async x =>
            {
                return await x.ConnectToPeripheral().FirstAsync();
            })
            .Subscribe(observer);
    });
}

return;
var source = new ActivitySource("Darp.Ble.Examples", "1.0.0");

using var listener = new ActivityListenerConfiguration()
    .InitialLevel.Override("Darp.Ble.Examples", LogEventLevel.Debug)
    .InitialLevel.Override(HciLoggingStrings.ActivityName, LogEventLevel.Debug)
    .TraceTo(logger);

var provider = new ServiceCollection()
    .AddLogging(builder => builder.AddSerilog(logger))
    .AddBleManager(builder =>
        builder
            .AddSerialHciHost("COM11", factory => factory.DeviceName = "Wooooowo")
            .AddSerialHciHost(factory =>
            {
                // Configure TimeProvider
                factory.TimeProvider = TimeProvider.System;
            })
            .AddMock(factory =>
            {
                // factory.TimeProvider = TimeProvider.System;
                // factory.DeviceName = "Local device";
                factory.AddPeripheral(
                    onInitialize: async device =>
                    {
                        await device.SetRandomAddressAsync(
                            new BleAddress(BleAddressType.RandomStatic, (UInt48)0xF1F2F3F4F5F6)
                        );
                        GattClientGattService gattService = device.Peripheral.AddGattService();
                        device.Peripheral.AddDeviceInformationService(
                            manufacturerName: "rosslight GmbH",
                            modelNumber: "ABC-123",
                            serialNumber: "12345",
                            hardwareRevision: "0.1.2",
                            firmwareRevision: "1.2.3",
                            softwareRevision: "2.3.4",
                            systemId: new SystemId(0x1234, 0x123456)
                        );
                        await device.Broadcaster.StartAdvertisingAsync(
                            type: BleEventType.AdvInd,
                            data: AdvertisingData
                                .Empty.WithCompleteLocalName(device.Name!)
                                .WithCompleteListOfServiceUuids(
                                    device.Peripheral.Services.Select(x => x.Uuid).ToArray()
                                ),
                            interval: ScanTiming.Ms1000
                        );
                    },
                    "Mock1"
                );
                factory.AddPeripheral(
                    onInitialize: async device =>
                    {
                        await device.SetRandomAddressAsync(
                            new BleAddress(BleAddressType.RandomStatic, (UInt48)0xF2F3F4F5F6F7)
                        );
                        await device.Broadcaster.StartAdvertisingAsync(
                            type: BleEventType.AdvNonConnInd,
                            data: AdvertisingData.Empty.WithCompleteLocalName(device.Name!),
                            interval: ScanTiming.Ms1000
                        );
                    },
                    "Mock2"
                );
                /*
                factory.AddPeerCentral(onInitialized: async device =>
                {
                    await using IGattServerPeer connectedPeer = await device
                        .Observer.RefCount()
                        .WhereConnectable()
                        .WhereService(BatteryServiceContract.BatteryService.Uuid)
                        .ConnectToPeripheral()
                        .FirstAsync();
                    var service = await connectedPeer.DiscoverDeviceInformationServiceAsync();
                    string name = await service.ManufacturerName!.ReadAsync();
                    string number = await service.ModelNumber!.ReadAsync();
                    var systemId = await service.SystemId!.ReadAsync();
                });*/
            })
    // .AddWinRT()
    )
    .BuildServiceProvider();

IBleDevice device = provider.GetRequiredService<BleManager>().EnumerateDevices().First();
await device.InitializeAsync();

await Observe(device);
return;

await device.SetRandomAddressAsync(new BleAddress(BleAddressType.RandomStatic, (UInt48)0xF2F2F3F4F5F6));
device.Peripheral.AddGattService();
device.Peripheral.AddDeviceInformationService(
    manufacturerName: "rosslight GmbH",
    modelNumber: "ABC-123",
    serialNumber: "12345",
    hardwareRevision: "0.1.2",
    //firmwareRevision: "1.2.3",
    softwareRevision: "2.3.4",
    systemId: new SystemId(0x1234, 0x123456)
);
device.Peripheral.AddEchoService(0x1234, 0x1235, 0x1236);
GattClientBatteryService batteryService = device.Peripheral.AddBatteryService();
_ = Observable
    .Interval(TimeSpan.FromMilliseconds(500))
    .SelectMany(async _ =>
    {
        int random = Random.Shared.Next(byte.MinValue, byte.MaxValue);
        return await batteryService.BatteryLevel.NotifyAllAsync((byte)random);
    })
    .Subscribe();

await device.Broadcaster.StartAdvertisingAsync(
    type: BleEventType.AdvInd,
    data: AdvertisingData
        .Empty.WithCompleteLocalName(device.Name!)
        .WithCompleteListOfServiceUuids(device.Peripheral.Services.Select(x => x.Uuid).ToArray()),
    interval: ScanTiming.Ms100,
    autoRestart: true
);

await Task.Delay(10000000);

return;

//await x.SetRandomAddressAsync(new BleAddress(BleAddressType.RandomStatic, (UInt48)0xF0F1F2F3F4F5));
await device.Observer.StartObservingAsync();
IGattServerPeer connectedPeer = await device
    .Observer.OnAdvertisement()
    .WhereConnectable()
    .WhereService(BatteryServiceContract.BatteryService.Uuid)
    .ConnectToPeripheral()
    .FirstAsync();
var service = await connectedPeer.DiscoverDeviceInformationServiceAsync();
string name = await service.ManufacturerName!.ReadAsync();
string number = await service.ModelNumber!.ReadAsync();
var systemId = await service.SystemId!.ReadAsync();
return;

await Advertise(device);

return;
async Task<IGattServerPeer> ConnectToDevice(IBleDevice device)
{
    await device.Observer.StartObservingAsync();
    var advertisement = await device.Observer.OnAdvertisement().WhereConnectable().FirstAsync();
    return await advertisement.ConnectToPeripheral().FirstAsync();
}
await Observe(device);
var counter = 0;
for (int ii = 0; ii < 1000; ii++)
{
    try
    {
        var peer1 = await ConnectToDevice(device);
        await peer1.DisposeAsync();
        Console.WriteLine($"Asd {ii}");
    }
    catch (Exception)
    {
        counter++;
    }
}
int i = 0;

async Task Observe(IBleDevice device)
{
    await device.Observer.StartObservingAsync();
    var advertisement = await device
        .Observer.OnAdvertisement()
        .WhereConnectable()
        .Where(a => a.Data.TryGetManufacturerSpecificData(CompanyIdentifiers.AampOfAmerica, out ReadOnlyMemory<byte> _))
        .FirstAsync();
    await using IGattServerPeer peer = await advertisement.ConnectToPeripheral().FirstAsync();
    var service = await peer.DiscoverEchoServiceAsync(0x1234, 0x1235, 0x1236);
    await using IAsyncDisposable _ = await service.EnableNotificationsAsync();
    var resultBytes = await service.QueryOneAsync(Convert.FromHexString("0011223344"));
    int i = 0;
}

async Task Advertise(IBleDevice device)
{
    var data = AdvertisingData.From([(AdTypes.CompleteLocalName, "Hello"u8.ToArray())]);
    IAdvertisingSet advertisingSet = await device.Broadcaster.CreateAdvertisingSetAsync(
        parameters: new AdvertisingParameters { MinPrimaryAdvertisingInterval = ScanTiming.Ms100 },
        data: data
    );
    IAsyncDisposable disp = await advertisingSet.StartAdvertisingAsync();
    for (var i = 0; i < 1000; i++)
    {
        await Task.Delay(2000);
        data = data.WithCompleteLocalName($"Hello {i}")
            .WithManufacturerSpecificData(CompanyIdentifiers.AppleInc, "AsdAsdAsd"u8.ToArray());
        await advertisingSet.SetAdvertisingDataAsync(data);
    }
    await Task.Delay(100000);
}

async Task AdvertiseApi(IBleDevice device)
{
    var resetPoint = DateTimeOffset.UtcNow;
    var measurementObservable = Observable
        .Interval(TimeSpan.FromSeconds(1))
        .Select(i => new HeartRateMeasurement((byte)(i % 0xFF))
        {
            EnergyExpended = (ushort)Math.Min((DateTimeOffset.UtcNow - resetPoint).TotalSeconds, 0xFFFF),
            IsSensorContactDetected = i % 2 is 0,
        });
    var xx = device.Peripheral.AddDeviceInformationService(
        manufacturerName: "rosslight GmbH",
        modelNumber: "ABC-123",
        serialNumber: "12345",
        hardwareRevision: "0.1.2",
        firmwareRevision: "1.2.3",
        softwareRevision: "2.3.4",
        systemId: new SystemId(0x1234, 0x123456)
    );
    var heartRateService = device.Peripheral.AddHeartRateService(
        HeartRateBodySensorLocation.Wrist,
        () => resetPoint = DateTimeOffset.UtcNow
    );
    if (heartRateService.BodySensorLocation is not null)
        await heartRateService.BodySensorLocation.UpdateValueAsync(HeartRateBodySensorLocation.Chest);

    device.Peripheral.AddEchoService(serviceUuid: 0x1234, writeUuid: 0x1234, notifyUuid: 0x1234);

    await device.Broadcaster.StartAdvertisingAsync(
        BleEventType.Connectable,
        data: AdvertisingData.From([
            (AdTypes.CompleteListOf16BitServiceOrServiceClassUuids, [0x34, 0x12]),
            (AdTypes.ManufacturerSpecificData, [0x04, 0x00, 0x01, 0x02, 0x03, 0x04]),
        ]),
        interval: ScanTiming.Ms100
    );
    IAdvertisingSet advertisingSet = await device.Broadcaster.CreateAdvertisingSetAsync();
    IAdvertisingSet advertisingSet2 = await device.Broadcaster.CreateAdvertisingSetAsync();
    // IPeriodicAdvertisingSet periodicAdvertisingSet = await x.Broadcaster.CreatePeriodicAdvertisingSetAsync();

    IAsyncDisposable disp = await advertisingSet.StartAdvertisingAsync();

    IAsyncDisposable disposable = await device.Broadcaster.StartAdvertisingAsync(advertisingSet, advertisingSet2);
    await device.Broadcaster.StartAdvertisingAsync(
        type: BleEventType.AdvInd,
        peerAddress: new BleAddress((UInt48)0x1234),
        data: AdvertisingData.From([(AdTypes.CompleteLocalName, "Hello"u8.ToArray())]),
        scanResponseData: null
    );

    await disposable.DisposeAsync();
}

await Task.Delay(20000000);
