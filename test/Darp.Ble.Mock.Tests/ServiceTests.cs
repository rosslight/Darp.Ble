using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Gatt.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Tests;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class ServiceTests(ILoggerFactory loggerFactory)
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    [Fact]
    public async Task DeviceInformationService_ShouldWork()
    {
        const string expectedManufacturerName = "rosslight";
        BleManager manager = new BleManagerBuilder()
            .AddMock(factory =>
                factory.AddPeripheral(async device =>
                {
                    GattClientDeviceInformationService service =
                        await device.Peripheral.AddDeviceInformationServiceAsync(
                            manufacturerName: expectedManufacturerName
                        );
                    await device.Broadcaster.StartAdvertisingAsync();
                })
            )
            .SetLogger(_loggerFactory)
            .CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync();

        IGapAdvertisement advertisement = await device.Observer.RefCount().FirstAsync();
        IGattServerPeer peer = await advertisement.ConnectToPeripheral().FirstAsync();
        var service = await peer.DiscoverDeviceInformationServiceAsync();

        string manufacturerName = await service.ManufacturerName!.ReadAsync();
        manufacturerName.Should().Be(expectedManufacturerName);
        service.ModelNumber.Should().BeNull();
        service.SerialNumber.Should().BeNull();
        service.HardwareRevision.Should().BeNull();
        service.FirmwareRevision.Should().BeNull();
        service.SoftwareRevision.Should().BeNull();
        service.SystemId.Should().BeNull();
        service.RegulatoryCertificationData.Should().BeNull();
    }

    [Fact]
    public async Task EchoService_ShouldWork()
    {
        BleUuid serviceUuid = 0x1234;
        BleUuid writeUuid = 0x1235;
        BleUuid notifyUuid = 0x1236;
        byte[] content = Convert.FromHexString("010203040506");

        BleManager manager = new BleManagerBuilder()
            .AddMock(factory =>
                factory.AddPeripheral(async device =>
                {
                    await device.Peripheral.AddEchoServiceAsync(serviceUuid, writeUuid, notifyUuid);
                    await device.Broadcaster.StartAdvertisingAsync();
                })
            )
            .SetLogger(_loggerFactory)
            .CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync();

        IGapAdvertisement advertisement = await device.Observer.RefCount().FirstAsync();
        await using IGattServerPeer peer = await advertisement.ConnectToPeripheral().FirstAsync();
        GattServerEchoService service = await peer.DiscoverEchoServiceAsync(serviceUuid, writeUuid, notifyUuid);

        byte[] response = await service.QueryOneAsync(content);

        response.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task HeartRateService_ShouldWork()
    {
        const byte expectedValue = 42;
        const bool expectedIsSensorContactDetected = true;
        const HeartRateBodySensorLocation expectedSensorLocation = HeartRateBodySensorLocation.Chest;

        BleManager manager = new BleManagerBuilder()
            .AddMock(factory =>
                factory.AddPeripheral(async device =>
                {
                    ushort energy = 0;
                    var heartRateSubject = new BehaviorSubject<HeartRateMeasurement>(default);
                    Observable
                        .Interval(TimeSpan.FromMilliseconds(100))
                        .Select(_ => new HeartRateMeasurement(expectedValue)
                        {
                            EnergyExpended = energy++,
                            IsSensorContactDetected = expectedIsSensorContactDetected,
                        })
                        .Subscribe(heartRateSubject);
                    GattClientHeartRateService service = await device.Peripheral.AddHeartRateServiceAsync(
                        expectedSensorLocation,
                        () => energy = 0
                    );
                    _ = heartRateSubject.Subscribe(measurement => service.HeartRateMeasurement.NotifyAll(measurement));
                    await device.Broadcaster.StartAdvertisingAsync(
                        data: AdvertisingData.Empty.WithCompleteListOfServiceUuids(device.Peripheral),
                        autoRestart: true
                    );
                    device.Peripheral.WhenConnected.Subscribe(clientPeer =>
                    {
                        // Notify subscribers of the current value as soon as they subscribe
                        service.HeartRateMeasurement.Notify(clientPeer, heartRateSubject.Value);
                    });
                })
            )
            .SetLogger(_loggerFactory)
            .CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync();

        IGapAdvertisement advertisement = await device.Observer.RefCount().Take(2).LastAsync();
        await using IGattServerPeer peer = await advertisement.ConnectToPeripheral().FirstAsync();
        GattServerHeartRateService service = await peer.DiscoverHeartRateServiceAsync();

        service.BodySensorLocation.Should().NotBeNull();
        HeartRateBodySensorLocation sensorLocation = await service.BodySensorLocation!.ReadAsync();
        sensorLocation.Should().Be(expectedSensorLocation);

        await using IDisposableObservable<HeartRateMeasurement> observable =
            await service.HeartRateMeasurement.OnNotifyAsync();

        service.HeartRateControlPoint.Should().NotBeNull();
        await service.HeartRateControlPoint!.WriteAsync([0x01]);
        HeartRateMeasurement measurement = await observable.FirstAsync();
        measurement.Value.Should().Be(expectedValue);
        measurement.EnergyExpended.Should().Be(0);
        measurement.IsSensorContactDetected.Should().Be(expectedIsSensorContactDetected);
    }

    [Fact]
    public async Task BatteryService_ShouldWork()
    {
        const byte expectedValue = 30;
        const string expectedUserDescription = "customString";

        BleManager manager = new BleManagerBuilder()
            .AddMock(factory =>
                factory.AddPeripheral(async device =>
                {
                    GattClientBatteryService service = await device.Peripheral.AddBatteryService(
                        batteryLevelDescription: expectedUserDescription
                    );
                    await device.Broadcaster.StartAdvertisingAsync(
                        data: AdvertisingData.Empty.WithCompleteListOfServiceUuids(device.Peripheral),
                        autoRestart: true
                    );
                    Observable
                        .Interval(TimeSpan.FromMilliseconds(100))
                        .Subscribe(_ => service.BatteryLevel.NotifyAll(expectedValue));
                    device.Peripheral.WhenConnected.Subscribe(clientPeer =>
                    {
                        // Notify subscribers of the current value as soon as they subscribe
                        service.BatteryLevel.Notify(clientPeer, expectedValue);
                    });
                })
            )
            .SetLogger(_loggerFactory)
            .CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync();

        IGapAdvertisement advertisement = await device.Observer.RefCount().FirstAsync();
        await using IGattServerPeer peer = await advertisement.ConnectToPeripheral().FirstAsync();
        GattServerBatteryService service = await peer.DiscoverBatteryServiceAsync();
        string userDescription = await service.BatteryLevel.ReadUserDescriptionAsync();
        userDescription.Should().Be(expectedUserDescription);
        var readLevel = await service.BatteryLevel.ReadAsync<byte>();
        readLevel.Should().Be(expectedValue);
        await using IDisposableObservable<byte> notifyable = await service.BatteryLevel.OnNotifyAsync<byte>();
        byte notifiedLevel = await notifyable.FirstAsync();
        notifiedLevel.Should().Be(readLevel);
    }
}
