using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Database;
using FluentAssertions;
using NSubstitute;
using IGattCharacteristicDeclaration = Darp.Ble.Gatt.Client.IGattCharacteristicDeclaration;

namespace Darp.Ble.Tests;

public sealed class GattDatabaseTests
{
    private static IGattClientService CreateService(BleUuid uuid, GattDatabaseCollection database)
    {
        var service = Substitute.For<IGattClientService>();
        service.Uuid.Returns(uuid);
        var serviceDeclaration = Substitute.For<IGattCharacteristicDeclaration>();
        serviceDeclaration.AttributeType.Returns(GattDatabaseCollection.PrimaryServiceType);
        serviceDeclaration.Handle.Returns(_ => database[serviceDeclaration]);
        service.Declaration.Returns(serviceDeclaration);
        return service;
    }

    private static IGattClientCharacteristic CreateCharacteristic(
        BleUuid uuid,
        IGattClientService service,
        GattDatabaseCollection database
    )
    {
        var characteristic = Substitute.For<IGattClientCharacteristic>();
        characteristic.Uuid.Returns(uuid);
        var characteristicDeclaration = Substitute.For<IGattCharacteristicDeclaration>();
        characteristicDeclaration.AttributeType.Returns(GattDatabaseCollection.CharacteristicType);
        characteristicDeclaration.Handle.Returns(_ => database[characteristicDeclaration]);
        characteristic.Declaration.Returns(characteristicDeclaration);
        var characteristicValue = Substitute.For<IGattCharacteristicValue>();
        characteristicValue.AttributeType.Returns(uuid);
        characteristicValue.Handle.Returns(_ => database[characteristicValue]);
        characteristic.Value.Returns(characteristicValue);
        characteristic.Service.Returns(service);
        return characteristic;
    }

    private static IGattClientDescriptor CreateDescriptor(
        BleUuid uuid,
        IGattClientCharacteristic characteristic,
        GattDatabaseCollection database
    )
    {
        var descriptor = Substitute.For<IGattClientDescriptor>();
        descriptor.Uuid.Returns(uuid);
        var descriptorValue = Substitute.For<IGattCharacteristicValue>();
        descriptorValue.AttributeType.Returns(DescriptorDeclaration.CharacteristicUserDescription.Uuid);
        descriptorValue.Handle.Returns(_ => database[descriptorValue]);
        descriptor.Value.Returns(descriptorValue);
        descriptor.Characteristic.Returns(characteristic);
        return descriptor;
    }

    [Fact]
    public void SingleService()
    {
        var database = new GattDatabaseCollection();

        IGattClientService service1 = CreateService(0x1234, database);
        database.AddService(service1);
        service1.Declaration.Handle.Should().Be(0x0001);
    }

    [Fact]
    public void MultipleServices()
    {
        var database = new GattDatabaseCollection();

        IGattClientService service1 = CreateService(0x1234, database);
        IGattClientService service2 = CreateService(0x1235, database);
        IGattClientService service3 = CreateService(0x1236, database);
        database.AddService(service1);
        database.AddService(service2);
        database.AddService(service3);
        service1.Declaration.Handle.Should().Be(0x0001);
        service2.Declaration.Handle.Should().Be(0x0002);
        service3.Declaration.Handle.Should().Be(0x0003);
    }

    [Fact]
    public void SingleCharacteristic()
    {
        var database = new GattDatabaseCollection();

        IGattClientService service1 = CreateService(0x1234, database);
        IGattClientCharacteristic characteristic1 = CreateCharacteristic(0x2234, service1, database);

        database.AddService(service1);
        database.AddCharacteristic(characteristic1);

        service1.Declaration.Handle.Should().Be(0x0001);
        characteristic1.Declaration.Handle.Should().Be(0x0002);
    }

    [Fact]
    public void MultipleCharacteristics()
    {
        var database = new GattDatabaseCollection();

        IGattClientService service1 = CreateService(0x1234, database);
        IGattClientCharacteristic characteristic1 = CreateCharacteristic(0x2234, service1, database);
        IGattClientCharacteristic characteristic2 = CreateCharacteristic(0x2235, service1, database);
        IGattClientCharacteristic characteristic3 = CreateCharacteristic(0x2236, service1, database);

        database.AddService(service1);
        database.AddCharacteristic(characteristic1);
        database.AddCharacteristic(characteristic2);
        database.AddCharacteristic(characteristic3);

        service1.Declaration.Handle.Should().Be(0x0001);
        characteristic1.Declaration.Handle.Should().Be(0x0002);
        characteristic2.Declaration.Handle.Should().Be(0x0004);
        characteristic3.Declaration.Handle.Should().Be(0x0006);
    }

    [Fact]
    public void SingleDescriptor()
    {
        var database = new GattDatabaseCollection();

        IGattClientService service1 = CreateService(0x1234, database);
        IGattClientCharacteristic characteristic1 = CreateCharacteristic(0x2234, service1, database);
        IGattClientDescriptor descriptor1 = CreateDescriptor(0x3234, characteristic1, database);

        database.AddService(service1);
        database.AddCharacteristic(characteristic1);
        database.AddDescriptor(descriptor1);

        service1.Declaration.Handle.Should().Be(0x0001);
        characteristic1.Declaration.Handle.Should().Be(0x0002);
        descriptor1.Value.Handle.Should().Be(0x0004);
    }

    [Fact]
    public void MultipleDescriptors()
    {
        var database = new GattDatabaseCollection();

        IGattClientService service1 = CreateService(0x1234, database);
        IGattClientCharacteristic characteristic1 = CreateCharacteristic(0x2234, service1, database);
        IGattClientDescriptor descriptor1 = CreateDescriptor(0x3234, characteristic1, database);
        IGattClientDescriptor descriptor2 = CreateDescriptor(0x3235, characteristic1, database);
        IGattClientDescriptor descriptor3 = CreateDescriptor(0x3236, characteristic1, database);

        database.AddService(service1);
        database.AddCharacteristic(characteristic1);
        database.AddDescriptor(descriptor1);
        database.AddDescriptor(descriptor2);
        database.AddDescriptor(descriptor3);

        service1.Declaration.Handle.Should().Be(0x0001);
        characteristic1.Declaration.Handle.Should().Be(0x0002);
        characteristic1.Value.Handle.Should().Be(0x0003);
        descriptor1.Value.Handle.Should().Be(0x0004);
        descriptor2.Value.Handle.Should().Be(0x0005);
        descriptor3.Value.Handle.Should().Be(0x0006);
    }

    [Fact]
    public void AddInRandomOrder()
    {
        var database = new GattDatabaseCollection();

        IGattClientService service1 = CreateService(0x1234, database);
        IGattClientCharacteristic characteristic1 = CreateCharacteristic(0x2234, service1, database);
        IGattClientDescriptor descriptor1 = CreateDescriptor(0x3234, characteristic1, database);
        IGattClientDescriptor descriptor2 = CreateDescriptor(0x3235, characteristic1, database);
        IGattClientCharacteristic characteristic2 = CreateCharacteristic(0x2235, service1, database);
        IGattClientService service2 = CreateService(0x1235, database);
        IGattClientCharacteristic characteristic3 = CreateCharacteristic(0x2236, service2, database);
        IGattClientDescriptor descriptor3 = CreateDescriptor(0x3236, characteristic3, database);

        database.AddService(service1);
        database.AddService(service2);
        database.AddCharacteristic(characteristic1);
        database.AddCharacteristic(characteristic2);
        database.AddCharacteristic(characteristic3);
        database.AddDescriptor(descriptor1);
        database.AddDescriptor(descriptor2);
        database.AddDescriptor(descriptor3);

        service1.Declaration.Handle.Should().Be(0x0001);
        characteristic1.Declaration.Handle.Should().Be(0x0002);
        characteristic1.Value.Handle.Should().Be(0x0003);
        descriptor1.Value.Handle.Should().Be(0x0004);
        descriptor2.Value.Handle.Should().Be(0x0005);
        characteristic2.Declaration.Handle.Should().Be(0x0006);
        characteristic2.Value.Handle.Should().Be(0x0007);
        service2.Declaration.Handle.Should().Be(0x0008);
        characteristic3.Declaration.Handle.Should().Be(0x0009);
        characteristic3.Value.Handle.Should().Be(0x000A);
        descriptor3.Value.Handle.Should().Be(0x000B);
    }

    [Fact]
    public void Enumerate()
    {
        var database = new GattDatabaseCollection();

        IGattClientService service1 = CreateService(0x1234, database);
        IGattClientCharacteristic characteristic1 = CreateCharacteristic(0x2234, service1, database);
        IGattClientDescriptor descriptor1 = CreateDescriptor(0x3234, characteristic1, database);
        IGattClientDescriptor descriptor2 = CreateDescriptor(0x3235, characteristic1, database);
        IGattClientCharacteristic characteristic2 = CreateCharacteristic(0x2235, service1, database);
        IGattClientService service2 = CreateService(0x1235, database);
        IGattClientCharacteristic characteristic3 = CreateCharacteristic(0x2236, service2, database);
        IGattClientDescriptor descriptor3 = CreateDescriptor(0x3236, characteristic3, database);

        database.AddService(service1);
        database.AddService(service2);
        database.AddCharacteristic(characteristic1);
        database.AddCharacteristic(characteristic2);
        database.AddCharacteristic(characteristic3);
        database.AddDescriptor(descriptor1);
        database.AddDescriptor(descriptor2);
        database.AddDescriptor(descriptor3);

        GattDatabaseEntry firstEntry = database.First();
        firstEntry.Handle.Should().Be(0x0001);
        firstEntry.AttributeType.Should().Be(GattDatabaseCollection.PrimaryServiceType);
        GattDatabaseEntry sixthEntry = database.Skip(5).First();
        sixthEntry.Handle.Should().Be(0x0006);
        sixthEntry.AttributeType.Should().Be(GattDatabaseCollection.CharacteristicType);
    }

    [Fact]
    public void GetServiceEntries()
    {
        var database = new GattDatabaseCollection();

        IGattClientService service1 = CreateService(0x1234, database);
        IGattClientCharacteristic characteristic1 = CreateCharacteristic(0x2234, service1, database);
        IGattClientDescriptor descriptor1 = CreateDescriptor(0x3234, characteristic1, database);
        IGattClientDescriptor descriptor2 = CreateDescriptor(0x3235, characteristic1, database);
        IGattClientCharacteristic characteristic2 = CreateCharacteristic(0x2235, service1, database);
        IGattClientService service2 = CreateService(0x1235, database);
        IGattClientCharacteristic characteristic3 = CreateCharacteristic(0x2236, service2, database);
        IGattClientDescriptor descriptor3 = CreateDescriptor(0x3236, characteristic3, database);

        database.AddService(service1);
        database.AddService(service2);
        database.AddCharacteristic(characteristic1);
        database.AddCharacteristic(characteristic2);
        database.AddCharacteristic(characteristic3);
        database.AddDescriptor(descriptor1);
        database.AddDescriptor(descriptor2);
        database.AddDescriptor(descriptor3);

        GattDatabaseGroupEntry[] services = database.GetServiceEntries(0x0001).ToArray();
        services[0].Handle.Should().Be(0x0001);
        services[0].AttributeType.Should().Be(GattDatabaseCollection.PrimaryServiceType);
        services[0].EndGroupHandle.Should().Be(0x0007);
        services[1].Handle.Should().Be(0x0008);
        services[1].AttributeType.Should().Be(GattDatabaseCollection.PrimaryServiceType);
        services[1].EndGroupHandle.Should().Be(0x000B);
    }
}
