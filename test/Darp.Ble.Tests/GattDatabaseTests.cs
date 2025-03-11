using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Database;
using FluentAssertions;
using NSubstitute;

namespace Darp.Ble.Tests;

public sealed class GattDatabaseTests
{
    private static IGattClientService CreateService(BleUuid uuid, GattDatabaseCollection database)
    {
        var service = Substitute.For<IGattClientService>();
        service.Uuid.Returns(uuid);
        var serviceDeclaration = Substitute.For<IGattServiceDeclaration>();
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

    private static IGattCharacteristicValue CreateDescriptor(BleUuid uuid, GattDatabaseCollection database)
    {
        var descriptorValue = Substitute.For<IGattCharacteristicValue>();
        descriptorValue.AttributeType.Returns(uuid);
        descriptorValue.Handle.Returns(_ => database[descriptorValue]);
        return descriptorValue;
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
        IGattCharacteristicValue descriptor1 = CreateDescriptor(0x3234, database);

        database.AddService(service1);
        database.AddCharacteristic(characteristic1);
        database.AddDescriptor(characteristic1, descriptor1);

        service1.Declaration.Handle.Should().Be(0x0001);
        characteristic1.Declaration.Handle.Should().Be(0x0002);
        descriptor1.Handle.Should().Be(0x0004);
    }

    [Fact]
    public void MultipleDescriptors()
    {
        var database = new GattDatabaseCollection();

        IGattClientService service1 = CreateService(0x1234, database);
        IGattClientCharacteristic characteristic1 = CreateCharacteristic(0x2234, service1, database);
        IGattCharacteristicValue descriptor1 = CreateDescriptor(0x3234, database);
        IGattCharacteristicValue descriptor2 = CreateDescriptor(0x3235, database);
        IGattCharacteristicValue descriptor3 = CreateDescriptor(0x3236, database);

        database.AddService(service1);
        database.AddCharacteristic(characteristic1);
        database.AddDescriptor(characteristic1, descriptor1);
        database.AddDescriptor(characteristic1, descriptor2);
        database.AddDescriptor(characteristic1, descriptor3);

        service1.Declaration.Handle.Should().Be(0x0001);
        characteristic1.Declaration.Handle.Should().Be(0x0002);
        characteristic1.Value.Handle.Should().Be(0x0003);
        descriptor1.Handle.Should().Be(0x0004);
        descriptor2.Handle.Should().Be(0x0005);
        descriptor3.Handle.Should().Be(0x0006);
    }

    [Fact]
    public void AddInRandomOrder()
    {
        var database = new GattDatabaseCollection();

        IGattClientService service1 = CreateService(0x1234, database);
        IGattClientCharacteristic characteristic1 = CreateCharacteristic(0x2234, service1, database);
        IGattCharacteristicValue descriptor1 = CreateDescriptor(0x3234, database);
        IGattCharacteristicValue descriptor2 = CreateDescriptor(0x3235, database);
        IGattClientCharacteristic characteristic2 = CreateCharacteristic(0x2235, service1, database);
        IGattClientService service2 = CreateService(0x1235, database);
        IGattClientCharacteristic characteristic3 = CreateCharacteristic(0x2236, service2, database);
        IGattCharacteristicValue descriptor3 = CreateDescriptor(0x3236, database);

        database.AddService(service1);
        database.AddService(service2);
        database.AddCharacteristic(characteristic1);
        database.AddCharacteristic(characteristic2);
        database.AddCharacteristic(characteristic3);
        database.AddDescriptor(characteristic1, descriptor1);
        database.AddDescriptor(characteristic1, descriptor2);
        database.AddDescriptor(characteristic3, descriptor3);

        service1.Declaration.Handle.Should().Be(0x0001);
        characteristic1.Declaration.Handle.Should().Be(0x0002);
        characteristic1.Value.Handle.Should().Be(0x0003);
        descriptor1.Handle.Should().Be(0x0004);
        descriptor2.Handle.Should().Be(0x0005);
        characteristic2.Declaration.Handle.Should().Be(0x0006);
        characteristic2.Value.Handle.Should().Be(0x0007);
        service2.Declaration.Handle.Should().Be(0x0008);
        characteristic3.Declaration.Handle.Should().Be(0x0009);
        characteristic3.Value.Handle.Should().Be(0x000A);
        descriptor3.Handle.Should().Be(0x000B);
    }

    [Fact]
    public void Enumerate()
    {
        var database = new GattDatabaseCollection();

        IGattClientService service1 = CreateService(0x1234, database);
        IGattClientCharacteristic characteristic1 = CreateCharacteristic(0x2234, service1, database);
        IGattCharacteristicValue descriptor1 = CreateDescriptor(0x3234, database);
        IGattCharacteristicValue descriptor2 = CreateDescriptor(0x3235, database);
        IGattClientCharacteristic characteristic2 = CreateCharacteristic(0x2235, service1, database);
        IGattClientService service2 = CreateService(0x1235, database);
        IGattClientCharacteristic characteristic3 = CreateCharacteristic(0x2236, service2, database);
        IGattCharacteristicValue descriptor3 = CreateDescriptor(0x3236, database);

        database.AddService(service1);
        database.AddService(service2);
        database.AddCharacteristic(characteristic1);
        database.AddCharacteristic(characteristic2);
        database.AddCharacteristic(characteristic3);
        database.AddDescriptor(characteristic1, descriptor1);
        database.AddDescriptor(characteristic1, descriptor2);
        database.AddDescriptor(characteristic3, descriptor3);

        GattDatabaseEntry service1Entry = database.First();
        service1Entry.Handle.Should().Be(0x0001);
        service1Entry.AttributeType.Should().Be(GattDatabaseCollection.PrimaryServiceType);
        service1Entry.IsGroupType.Should().BeTrue();
        service1Entry.TryGetGroupEndHandle(out ushort firstGroupHandle).Should().BeTrue();
        firstGroupHandle.Should().Be(0x0007);

        GattDatabaseEntry characteristic1ValueEntry = database.Skip(2).First();
        characteristic1ValueEntry.Handle.Should().Be(0x0003);
        characteristic1ValueEntry.AttributeType.Should().Be(BleUuid.FromUInt16(0x2234));
        characteristic1ValueEntry.IsGroupType.Should().BeFalse();
        characteristic1ValueEntry
            .TryGetGroupEndHandle(out ushort characteristic1ValueEndGroupHandle)
            .Should()
            .BeFalse();
        characteristic1ValueEndGroupHandle.Should().Be(0x0003);

        GattDatabaseEntry characteristic2Entry = database.Skip(5).First();
        characteristic2Entry.Handle.Should().Be(0x0006);
        characteristic2Entry.AttributeType.Should().Be(GattDatabaseCollection.CharacteristicType);
        characteristic2Entry.IsGroupType.Should().BeTrue();
        characteristic2Entry.TryGetGroupEndHandle(out ushort sixthGroupHandle).Should().BeTrue();
        sixthGroupHandle.Should().Be(0x0007);
    }

    [Fact]
    public void GetServiceEntries()
    {
        var database = new GattDatabaseCollection();

        IGattClientService service1 = CreateService(0x1234, database);
        IGattClientCharacteristic characteristic1 = CreateCharacteristic(0x2234, service1, database);
        IGattCharacteristicValue descriptor1 = CreateDescriptor(0x3234, database);
        IGattCharacteristicValue descriptor2 = CreateDescriptor(0x3235, database);
        IGattClientCharacteristic characteristic2 = CreateCharacteristic(0x2235, service1, database);
        IGattClientService service2 = CreateService(0x1235, database);
        IGattClientCharacteristic characteristic3 = CreateCharacteristic(0x2236, service2, database);
        IGattCharacteristicValue descriptor3 = CreateDescriptor(0x3236, database);

        database.AddService(service1);
        database.AddService(service2);
        database.AddCharacteristic(characteristic1);
        database.AddCharacteristic(characteristic2);
        database.AddCharacteristic(characteristic3);
        database.AddDescriptor(characteristic1, descriptor1);
        database.AddDescriptor(characteristic1, descriptor2);
        database.AddDescriptor(characteristic3, descriptor3);

        GattDatabaseGroupEntry[] services = database.GetServiceEntries(0x0001).ToArray();
        services[0].Handle.Should().Be(0x0001);
        services[0].AttributeType.Should().Be(GattDatabaseCollection.PrimaryServiceType);
        services[0].EndGroupHandle.Should().Be(0x0007);
        services[1].Handle.Should().Be(0x0008);
        services[1].AttributeType.Should().Be(GattDatabaseCollection.PrimaryServiceType);
        services[1].EndGroupHandle.Should().Be(0x000B);
    }
}
