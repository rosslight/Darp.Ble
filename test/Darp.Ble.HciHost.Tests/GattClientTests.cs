using System.Buffers.Binary;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Verify;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

/// <summary> Using the services of a peripheral from an HCI adapter </summary>
public sealed class GattClientTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private ushort _level;
    private ushort _control;
    private ushort _description;

    private FakePeripheral Battery(FakeController controller, ushort mtu = 65) =>
        new FakePeripheral(controller, 0x0001) { Mtu = mtu }
            .AddService(
                0x180F,
                service =>
                {
                    _level = service.AddCharacteristic(0x2A19, GattProperty.Read | GattProperty.Notify, [80]);
                    _description = service.AddDescriptor(0x2901, "Main"u8.ToArray());
                    _control = service.AddCharacteristic(
                        0xFF01,
                        GattProperty.Write | GattProperty.WriteWithoutResponse
                    );
                }
            )
            .AddService(0x180A, service => service.AddCharacteristic(0x2A29, GattProperty.Read, "Darp"u8.ToArray()));

    [Fact]
    public async Task Discovery_FindsTheServicesCharacteristicsAndDescriptors()
    {
        await using var controller = new FakeController();
        FakePeripheral peripheral = Battery(controller);
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);

        IReadOnlyList<RemoteGattService> services = await connection.DiscoverServicesAsync(Token);
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await services[0].DiscoverCharacteristicsAsync(Token);
        IReadOnlyList<RemoteDescriptor> descriptors = await characteristics[0].DiscoverDescriptorsAsync(Token);

        services.Select(service => service.Uuid).ShouldBe([BleUuid.FromUInt16(0x180F), BleUuid.FromUInt16(0x180A)]);
        characteristics
            .Select(c => (c.Uuid, c.Properties))
            .ShouldBe([
                (BleUuid.FromUInt16(0x2A19), GattProperty.Read | GattProperty.Notify),
                (BleUuid.FromUInt16(0xFF01), GattProperty.Write | GattProperty.WriteWithoutResponse),
            ]);
        descriptors.Select(d => d.Uuid).ShouldBe([BleUuid.FromUInt16(0x2902), BleUuid.FromUInt16(0x2901)]);
    }

    [Fact]
    public async Task Discovery_FindsA32BitUuid_ThatTheAttProtocolSendsAs128Bits()
    {
        BleUuid declared = BleUuid.FromUInt32(0x12345678);
        await using var controller = new FakeController();
        FakePeripheral peripheral = new FakePeripheral(controller, 0x0001).AddService(
            BleUuid.FromGuid(declared.Value),
            service => service.AddCharacteristic(0x2A19, GattProperty.Read)
        );
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);

        RemoteGattService service = await connection.DiscoverServiceAsync(declared, Token);

        service.Uuid.ShouldBe(declared);
    }

    [Fact]
    public async Task DiscoveryOfManyCharacteristics_TakesSeveralPages()
    {
        await using var controller = new FakeController();
        FakePeripheral peripheral = new FakePeripheral(controller, 0x0001) { Mtu = 23 }.AddService(
            0x180F,
            service =>
            {
                for (ushort uuid = 0xFF01; uuid <= 0xFF05; uuid++)
                    service.AddCharacteristic(uuid, GattProperty.Read);
            }
        );
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);

        RemoteGattService service = await connection.DiscoverServiceAsync(0x180F, Token);
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await service.DiscoverCharacteristicsAsync(Token);

        characteristics
            .Select(c => c.Uuid)
            .ShouldBe([.. Enumerable.Range(0xFF01, 5).Select(u => BleUuid.FromUInt16((ushort)u))]);
        peripheral.Received.Count(pdu => pdu[0] == (byte)AttOpCode.ATT_READ_BY_TYPE_REQ).ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task ACancelledCharacteristicDiscovery_LeavesNoWrongDescriptorRanges()
    {
        await using var controller = new FakeController();
        FakePeripheral peripheral = new FakePeripheral(controller, 0x0001) { Mtu = 23 }.AddService(
            0x180F,
            service =>
            {
                for (ushort uuid = 0xFF01; uuid <= 0xFF05; uuid++)
                    service.AddCharacteristic(uuid, GattProperty.Read);
            }
        );
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);
        RemoteGattService service = await connection.DiscoverServiceAsync(0x180F, Token);
        peripheral.HoldAnswers = true;
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Token);
        Task discovering = service.DiscoverCharacteristicsAsync(cancel.Token);
        await peripheral.WaitForReceivedAsync(received =>
            received.Any(pdu => pdu[0] == (byte)AttOpCode.ATT_READ_BY_TYPE_REQ)
        );

        await cancel.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(discovering);
        peripheral.HoldAnswers = false;
        await peripheral.ReleaseAsync();
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await service.DiscoverCharacteristicsAsync(Token);

        (await characteristics[2].DiscoverDescriptorsAsync(Token)).ShouldBeEmpty();
    }

    [Fact]
    public async Task ReadingAndWriting_UseTheCharacteristicsHandles()
    {
        await using var controller = new FakeController();
        FakePeripheral peripheral = Battery(controller);
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await (
            await connection.DiscoverServiceAsync(0x180F, Token)
        ).DiscoverCharacteristicsAsync(Token);

        byte[] level = await characteristics[0].ReadAsync(Token);
        await characteristics[1].WriteAsync(new byte[] { 1, 2 }, Token);

        level.ShouldBe([80]);
        peripheral.ValueOf(_control).ShouldBe([1, 2]);
        peripheral.Received[^1][0].ShouldBe((byte)AttOpCode.ATT_WRITE_REQ);
    }

    [Fact]
    public async Task WritingWithoutResponse_SendsAWriteCommand()
    {
        await using var controller = new FakeController();
        FakePeripheral peripheral = Battery(controller);
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await (
            await connection.DiscoverServiceAsync(0x180F, Token)
        ).DiscoverCharacteristicsAsync(Token);

        await characteristics[1].WriteWithoutResponseAsync(new byte[] { 3 }, Token);
        // A read after it shows the command arrived first
        await characteristics[0].ReadAsync(Token);

        peripheral.Received[^2].ShouldBe([(byte)AttOpCode.ATT_WRITE_CMD, (byte)_control, 0x00, 3]);
    }

    [Fact]
    public async Task AnAttError_IsAGattExceptionWithItsStatus()
    {
        await using var controller = new FakeController();
        FakePeripheral peripheral = Battery(controller);
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await (
            await connection.DiscoverServiceAsync(0x180F, Token)
        ).DiscoverCharacteristicsAsync(Token);
        peripheral.Fail(_level, AttErrorCode.ReadNotPermittedError);

        GattException exception = await Should.ThrowAsync<GattException>(() => characteristics[0].ReadAsync(Token));

        exception.AttStatus.ShouldBe(GattProtocolStatus.ReadNotPermitted);
    }

    // Device information at the default MTU, where an answer carries at most 22 bytes: a characteristic and its
    // descriptor with the given value, and a short characteristic
    private static FakePeripheral LongValues(FakeController controller, byte[] value) =>
        new FakePeripheral(controller, 0x0001) { Mtu = 23 }.AddService(
            0x180A,
            service =>
            {
                service.AddCharacteristic(0x2A29, GattProperty.Read, value);
                service.AddDescriptor(0x2901, value);
                service.AddCharacteristic(0x2A24, GattProperty.Read, [1]);
            }
        );

    private static byte[] Counting(int length) => [.. Enumerable.Range(0, length).Select(i => (byte)i)];

    // A value that ends in a shorter answer, one that ends exactly where an answer ends, one that fills the first answer
    [Theory]
    [InlineData(40)]
    [InlineData(44)]
    [InlineData(22)]
    public async Task AValueLongerThanOneAnswer_IsReadWhole(int length)
    {
        await using var controller = new FakeController();
        FakePeripheral peripheral = LongValues(controller, Counting(length));
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);
        RemoteGattCharacteristic characteristic = (
            await (await connection.DiscoverServiceAsync(0x180A, Token)).DiscoverCharacteristicsAsync(Token)
        )[0];
        RemoteDescriptor descriptor = (await characteristic.DiscoverDescriptorsAsync(Token))[0];

        byte[] value = await characteristic.ReadAsync(Token);
        byte[] descriptorValue = await descriptor.ReadAsync(Token);

        value.ShouldBe(Counting(length));
        descriptorValue.ShouldBe(Counting(length));
    }

    [Fact]
    public async Task AValueLongerThanAnAttributeMayBe_IsReadUpToTheLongestAttributeValue()
    {
        await using var controller = new FakeController();
        FakePeripheral peripheral = LongValues(controller, Counting(600));
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);
        RemoteGattCharacteristic characteristic = (
            await (await connection.DiscoverServiceAsync(0x180A, Token)).DiscoverCharacteristicsAsync(Token)
        )[0];

        byte[] value = await characteristic.ReadAsync(Token);

        value.ShouldBe(Counting(512));
        peripheral
            .Received.Where(pdu => pdu[0] == (byte)AttOpCode.ATT_READ_BLOB_REQ)
            .Select(pdu => BinaryPrimitives.ReadUInt16LittleEndian(pdu.AsSpan(3)))
            .ShouldAllBe(offset => offset < 512);
    }

    [Fact]
    public async Task AValueLongerThanAnAttributeMayBe_IsCutToTheLongestAttributeValue_AlsoWhenOneAnswerHoldsIt()
    {
        await using var controller = new FakeController();
        // At MTU 517 one answer carries up to 516 bytes
        FakePeripheral peripheral = new FakePeripheral(controller, 0x0001) { Mtu = 517 }.AddService(
            0x180A,
            service => service.AddCharacteristic(0x2A29, GattProperty.Read, Counting(513))
        );
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);
        RemoteGattCharacteristic characteristic = (
            await (await connection.DiscoverServiceAsync(0x180A, Token)).DiscoverCharacteristicsAsync(Token)
        )[0];

        byte[] value = await characteristic.ReadAsync(Token);

        value.ShouldBe(Counting(512));
    }

    [Fact]
    public async Task ALongReadCancelledAfterItWasSent_SendsNoFurtherTransaction()
    {
        await using var controller = new FakeController();
        FakePeripheral peripheral = LongValues(controller, Counting(40));
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);
        IReadOnlyList<RemoteGattCharacteristic> characteristics = await (
            await connection.DiscoverServiceAsync(0x180A, Token)
        ).DiscoverCharacteristicsAsync(Token);
        peripheral.HoldAnswers = true;
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Token);
        Task<byte[]> cancelled = characteristics[0].ReadAsync(cancel.Token);
        await peripheral.WaitForReceivedAsync(received => received.Any(pdu => pdu[0] == (byte)AttOpCode.ATT_READ_REQ));

        await cancel.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(cancelled);
        peripheral.HoldAnswers = false;
        await peripheral.ReleaseAsync();
        // The next read goes out once the cancelled one ended
        byte[] next = await characteristics[1].ReadAsync(Token);

        next.ShouldBe([1]);
        peripheral
            .Received.Where(pdu => pdu[0] is (byte)AttOpCode.ATT_READ_REQ or (byte)AttOpCode.ATT_READ_BLOB_REQ)
            .Select(pdu => (AttOpCode)pdu[0])
            .ShouldBe([AttOpCode.ATT_READ_REQ, AttOpCode.ATT_READ_REQ]);
    }

    [Fact]
    public async Task Notifications_AreTurnedOnInTheCccd_AndReachTheStream()
    {
        await using var controller = new FakeController();
        FakePeripheral peripheral = Battery(controller);
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);
        RemoteGattCharacteristic level = (
            await (await connection.DiscoverServiceAsync(0x180F, Token)).DiscoverCharacteristicsAsync(Token)
        )[0];
        using NotificationStream<byte[]> stream = level.Subscribe();

        await level.EnableNotificationsAsync(Token);
        await peripheral.NotifyAsync(_level, [79]);

        peripheral.ValueOf((ushort)(_level + 1)).ShouldBe([0x01, 0x00]);
        (await stream.FirstAsync(Token)).ShouldBe([79]);
    }

    [Fact]
    public async Task EnablingCancelledWhileTheCccdIsLookedUp_DoesNotCountAsOn()
    {
        await using var controller = new FakeController();
        FakePeripheral peripheral = Battery(controller);
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);
        RemoteGattCharacteristic level = (
            await (await connection.DiscoverServiceAsync(0x180F, Token)).DiscoverCharacteristicsAsync(Token)
        )[0];
        peripheral.HoldAnswers = true;
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Token);
        Task enabling = level.EnableNotificationsAsync(cancel.Token);
        await peripheral.WaitForReceivedAsync(received =>
            received.Any(pdu => pdu[0] == (byte)AttOpCode.ATT_FIND_INFORMATION_REQ)
        );

        await cancel.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(enabling);
        peripheral.HoldAnswers = false;
        await peripheral.ReleaseAsync();
        await level.EnableNotificationsAsync(Token);

        peripheral.ValueOf((ushort)(_level + 1)).ShouldBe([0x01, 0x00]);
    }

    [Fact]
    public async Task Descriptors_AreReadAndWritten()
    {
        await using var controller = new FakeController();
        FakePeripheral peripheral = Battery(controller);
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);
        RemoteGattCharacteristic level = (
            await (await connection.DiscoverServiceAsync(0x180F, Token)).DiscoverCharacteristicsAsync(Token)
        )[0];
        IReadOnlyList<RemoteDescriptor> descriptors = await level.DiscoverDescriptorsAsync(Token);

        byte[] description = await descriptors[1].ReadAsync(Token);
        await descriptors[1].WriteAsync("Side"u8.ToArray(), Token);

        description.ShouldBe("Main"u8.ToArray());
        peripheral.ValueOf(_description).ShouldBe("Side"u8.ToArray());
    }
}
