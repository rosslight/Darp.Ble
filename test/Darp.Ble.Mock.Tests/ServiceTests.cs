using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Gatt.Services;
using Darp.Ble.Mock.Testing;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Tests;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public sealed class ServiceTests(ILoggerFactory loggerFactory)
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    [Fact]
    public async Task GapService_ShouldWork()
    {
        const string expectedDeviceName = "Some device name";

        GattServerGapService service = await MockHelpers.CreateMockedService(
            peripheral => peripheral.Device.Name = expectedDeviceName,
            peer => peer.DiscoverGapServiceAsync(),
            _loggerFactory
        );

        string deviceName = await service.DeviceName.ReadAsync();
        deviceName.Should().Be(expectedDeviceName);
        AppearanceValues appearance = await service.Appearance.ReadAsync();
        appearance.Should().Be(AppearanceValues.Unknown);
    }

    [Fact]
    public async Task DeviceInformationService_ShouldWork()
    {
        const string expectedManufacturerName = "rosslight";

        GattServerDeviceInformationService service = await MockHelpers.CreateMockedService(
            peripheral => peripheral.AddDeviceInformationService(expectedManufacturerName),
            peer => peer.DiscoverDeviceInformationServiceAsync(),
            _loggerFactory
        );

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

        GattServerEchoService service = await MockHelpers.CreateMockedService(
            peripheral => peripheral.AddEchoService(serviceUuid, writeUuid, notifyUuid),
            peer => peer.DiscoverEchoServiceAsync(serviceUuid, writeUuid, notifyUuid),
            _loggerFactory
        );

        byte[] response = await service.QueryOneAsync(content);

        response.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task HeartRateService_ShouldWork()
    {
        const byte expectedValue = 42;
        const bool expectedIsSensorContactDetected = true;
        const HeartRateBodySensorLocation expectedSensorLocation = HeartRateBodySensorLocation.Chest;

        IGattServerPeer peer = await MockHelpers.CreateMockedPeerDevice(
            peripheral =>
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
                GattClientHeartRateService service = peripheral.AddHeartRateService(
                    expectedSensorLocation,
                    () => energy = 0
                );
                _ = heartRateSubject
                    .SelectMany(async measurement => await service.HeartRateMeasurement.NotifyAllAsync(measurement))
                    .Subscribe();
                // Notify subscribers of the current value as soon as they subscribe
                _ = peripheral
                    .WhenConnected.SelectMany(async clientPeer =>
                        await service.HeartRateMeasurement.NotifyAsync(clientPeer, heartRateSubject.Value)
                    )
                    .Subscribe();
            },
            _loggerFactory
        );
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

        IGattServerPeer peer = await MockHelpers.CreateMockedPeerDevice(
            peripheral =>
            {
                GattClientBatteryService service = peripheral.AddBatteryService(
                    batteryLevelDescription: expectedUserDescription
                );
                Observable
                    .Interval(TimeSpan.FromMilliseconds(100))
                    .SelectMany(async _ => await service.BatteryLevel.NotifyAllAsync(expectedValue))
                    .Subscribe();
                // Notify subscribers of the current value as soon as they subscribe
                peripheral
                    .WhenConnected.SelectMany(async clientPeer =>
                        await service.BatteryLevel.NotifyAsync(clientPeer, expectedValue)
                    )
                    .Subscribe();
            },
            _loggerFactory
        );
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
