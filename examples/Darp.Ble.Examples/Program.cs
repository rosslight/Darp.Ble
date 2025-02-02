// See https://aka.ms/new-console-template for more information

using System.Globalization;
using System.Reactive.Linq;
using Darp.Ble;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Gatt.Services;
using Darp.Ble.HciHost;
using Darp.Ble.Mock;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

Logger logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

new ServiceCollection()
    .AddLogging(builder => builder.AddSerilog(logger))
    .AddBleManager(builder => builder.AddHciHost());

BleManager manager = new BleManagerBuilder()
    // .AddHciHost()
    // .AddWinRT()
    .AddMock(factory =>
        factory.AddPeripheral(
            async device =>
            {
                await device.SetRandomAddressAsync(new BleAddress(BleAddressType.RandomStatic, (UInt48)0xF1F2F3F4F5F6));
                await device.Peripheral.AddDeviceInformationServiceAsync(
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
                        .WithCompleteListOfServiceUuids(device.Peripheral.Services.Select(x => x.Uuid).ToArray()),
                    interval: ScanTiming.Ms1000
                );
            },
            "Mock1"
        )
    )
    .SetLogger(new SerilogLoggerFactory(logger))
    .CreateManager();

IBleDevice x = manager.EnumerateDevices().First();
await x.InitializeAsync();

//await x.SetRandomAddressAsync(new BleAddress(BleAddressType.RandomStatic, (UInt48)0xF0F1F2F3F4F5));
IGattServerPeer connectedPeer = await ConnectToDevice(x);

var service = await connectedPeer.DiscoverDeviceInformationServiceAsync();
string name = await service.ManufacturerName!.ReadAsync();
string number = await service.ModelNumber!.ReadAsync();
var systemId = await service.SystemId!.ReadAsync();
return;

await Advertise(x);

return;
async Task<IGattServerPeer> ConnectToDevice(IBleDevice device)
{
    var advertisement = await device.Observer.RefCount().WhereConnectable().FirstAsync();
    return await advertisement.ConnectToPeripheral().FirstAsync();
}
await Observe(x);
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

async Task Observe(IBleDevice device)
{
    var advertisement = await device
        .Observer.RefCount()
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
    var xx = await device.Peripheral.AddDeviceInformationServiceAsync(
        manufacturerName: "rosslight GmbH",
        modelNumber: "ABC-123",
        serialNumber: "12345",
        hardwareRevision: "0.1.2",
        firmwareRevision: "1.2.3",
        softwareRevision: "2.3.4",
        systemId: new SystemId(0x1234, 0x123456)
    );
    var heartRateService = await device.Peripheral.AddHeartRateServiceAsync(
        HeartRateBodySensorLocation.Wrist,
        () => resetPoint = DateTimeOffset.UtcNow
    );
    heartRateService.BodySensorLocation?.UpdateValue(HeartRateBodySensorLocation.Chest);

    await device.Peripheral.AddEchoServiceAsync(serviceUuid: 0x1234, writeUuid: 0x1234, notifyUuid: 0x1234);

    await device.Broadcaster.StartAdvertisingAsync(
        BleEventType.Connectable,
        data: AdvertisingData.From(
            [
                (AdTypes.CompleteListOf16BitServiceOrServiceClassUuids, [0x34, 0x12]),
                (AdTypes.ManufacturerSpecificData, [0x04, 0x00, 0x01, 0x02, 0x03, 0x04]),
            ]
        ),
        interval: ScanTiming.Ms100
    );
    IAdvertisingSet advertisingSet = await x.Broadcaster.CreateAdvertisingSetAsync();
    IAdvertisingSet advertisingSet2 = await x.Broadcaster.CreateAdvertisingSetAsync();
    // IPeriodicAdvertisingSet periodicAdvertisingSet = await x.Broadcaster.CreatePeriodicAdvertisingSetAsync();

    IAsyncDisposable disp = await advertisingSet.StartAdvertisingAsync();

    IAsyncDisposable disposable = await x.Broadcaster.StartAdvertisingAsync(advertisingSet, advertisingSet2);
    await device.Broadcaster.StartAdvertisingAsync(
        type: BleEventType.AdvInd,
        peerAddress: new BleAddress((UInt48)0x1234),
        data: AdvertisingData.From([(AdTypes.CompleteLocalName, "Hello"u8.ToArray())]),
        scanResponseData: null
    );

    await disposable.DisposeAsync();
}

await Task.Delay(20000000);
