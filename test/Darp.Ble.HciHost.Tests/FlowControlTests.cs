using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Verify;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

/// <summary> Sharing the controller's 3 ACL buffers of 251 bytes between links (seam 2 of PR 6, D8) </summary>
public sealed class FlowControlTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<(
        BleAdapter Adapter,
        RemoteGattCharacteristic Characteristic,
        FakePeripheral Peripheral
    )> ConnectAsync(
        FakeController controller,
        BleAdapter? adapter = null,
        ushort handle = 0x0001,
        TimeProvider? time = null
    )
    {
        adapter ??= await HciAdapter.OpenAsync(controller, time);
        var peripheral = new FakePeripheral(controller, handle) { Mtu = 517 }.AddService(
            0x180F,
            service => service.AddCharacteristic(0xFF01, GattProperty.WriteWithoutResponse | GattProperty.Read)
        );
        _ = FakePeripheral.ServeAsync(controller, peripheral);
        Task<ConnectedPeripheral> connecting = adapter.Central.ConnectAsync(HciAdapter.Peer, cancellationToken: Token);
        // The n-th link is the n-th connection attempt
        await controller.WaitForCommandAsync(
            Hci.Package.HciOpCode.HCI_LE_Extended_Create_Connection_V1,
            occurrence: handle
        );
        await controller.PushAsync(HciAdapter.ConnectionComplete(handle));
        ConnectedPeripheral connection = await connecting;
        RemoteGattCharacteristic characteristic = (
            await (await connection.Gatt.DiscoverServiceAsync(0x180F, Token)).DiscoverCharacteristicsAsync(Token)
        )[0];
        return (adapter, characteristic, peripheral);
    }

    [Fact]
    public async Task AWriteWithoutResponse_CompletesOnceTheControllerHasABufferForIt()
    {
        await using var controller = new FakeController();
        (BleAdapter adapter, RemoteGattCharacteristic characteristic, _) = await ConnectAsync(controller);
        await using BleAdapter owned = adapter;
        controller.CompletePacketsAutomatically = false;

        for (byte i = 0; i < 3; i++)
            characteristic.WriteWithoutResponseAsync(new[] { i }, Token).IsCompletedSuccessfully.ShouldBeTrue();
        ValueTask fourth = characteristic.WriteWithoutResponseAsync(new byte[] { 3 }, Token);
        fourth.IsCompleted.ShouldBeFalse();
        await controller.CompletePacketsAsync(0x0001);

        await fourth;
    }

    [Fact]
    public async Task AWriteCancelledWhileWaitingForABuffer_IsNeverSent()
    {
        await using var controller = new FakeController();
        (BleAdapter adapter, RemoteGattCharacteristic characteristic, FakePeripheral peripheral) = await ConnectAsync(
            controller
        );
        await using BleAdapter owned = adapter;
        controller.CompletePacketsAutomatically = false;
        for (byte i = 0; i < 3; i++)
            await characteristic.WriteWithoutResponseAsync(new[] { i }, Token);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Token);
        ValueTask cancelled = characteristic.WriteWithoutResponseAsync(new byte[] { 0xCC }, cancel.Token);

        await cancel.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(cancelled.AsTask());
        await controller.CompletePacketsAsync(0x0001, 3);
        controller.CompletePacketsAutomatically = true;
        await characteristic.WriteWithoutResponseAsync(new byte[] { 0xDD }, Token);
        await characteristic.ReadAsync(Token);

        byte[][] commands = [.. peripheral.Received.Where(pdu => pdu[0] == (byte)AttOpCode.ATT_WRITE_CMD)];
        commands.Select(pdu => pdu[3]).ShouldBe([0, 1, 2, 0xDD]);
    }

    [Fact]
    public async Task CancellingARequestJustAsItGetsABuffer_NeverBlocks()
    {
        await using var controller = new FakeController();
        (BleAdapter adapter, RemoteGattCharacteristic characteristic, _) = await ConnectAsync(controller);
        await using BleAdapter owned = adapter;
        controller.CompletePacketsAutomatically = false;
        for (byte i = 0; i < 3; i++)
            await characteristic.WriteWithoutResponseAsync(new[] { i }, Token);

        for (var i = 0; i < 200; i++)
        {
            int readsBefore = Reads(controller);
            using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Token);
            // Waits for a buffer
            Task<byte[]> read = characteristic.ReadAsync(cancel.Token);
            Task cancelling = Task.Run(() => cancel.CancelAsync(), Token);
            Task freeing = Task.Run(() => controller.CompletePacketsAsync(0x0001), Token);
            await Task.WhenAll(cancelling, freeing).WaitAsync(TimeSpan.FromSeconds(5), Token);
            await read.ContinueWith(_ => { }, TaskScheduler.Default).WaitAsync(TimeSpan.FromSeconds(5), Token);
            // A read that stayed queued left the freed buffer; a write takes it, so the next read waits again
            if (Reads(controller) == readsBefore)
                await characteristic.WriteWithoutResponseAsync(new byte[] { 0 }, Token);
        }
    }

    private static int Reads(FakeController controller) =>
        controller.AclPackets.Count(packet => packet.Packet[8] == (byte)AttOpCode.ATT_READ_REQ);

    [Fact]
    public async Task AWriteWaitingForABuffer_DoesNotHoldUpTheEndOfItsConnection()
    {
        await using var controller = new FakeController();
        var time = new Microsoft.Extensions.Time.Testing.FakeTimeProvider();
        (BleAdapter adapter, RemoteGattCharacteristic characteristic, _) = await ConnectAsync(controller, time: time);
        await using BleAdapter owned = adapter;
        ConnectedPeripheral connection = adapter.Central.Connections.Single();
        controller.CompletePacketsAutomatically = false;
        controller.Hold(Hci.Package.HciOpCode.HCI_Disconnect);
        for (byte i = 0; i < 3; i++)
            await characteristic.WriteWithoutResponseAsync(new[] { i }, Token);
        ValueTask waiting = characteristic.WriteWithoutResponseAsync(new byte[] { 4 }, Token);

        Task disposing = connection.DisposeAsync().AsTask();
        await controller.WaitForCommandAsync(Hci.Package.HciOpCode.HCI_Disconnect);
        time.Advance(TimeSpan.FromSeconds(5));

        await Should.ThrowAsync<BleDisconnectedException>(waiting.AsTask());
        await disposing;
    }

    [Fact]
    public async Task ALongWrite_IsSentInFragments_AndTheCallerMayReuseItsBufferOnceItCompleted()
    {
        await using var controller = new FakeController();
        (BleAdapter adapter, RemoteGattCharacteristic characteristic, FakePeripheral peripheral) = await ConnectAsync(
            controller
        );
        await using BleAdapter owned = adapter;
        controller.CompletePacketsAutomatically = false;
        // One buffer left for the first fragment
        await characteristic.WriteWithoutResponseAsync(new byte[] { 1 }, Token);
        await characteristic.WriteWithoutResponseAsync(new byte[] { 2 }, Token);
        byte[] value = [.. Enumerable.Range(0, 300).Select(i => (byte)i)];
        byte[] expected = [.. value];

        await characteristic.WriteWithoutResponseAsync(value, Token);
        Array.Clear(value);
        controller.CompletePacketsAutomatically = true;
        await controller.CompletePacketsAsync(0x0001, 3);
        await characteristic.ReadAsync(Token);

        byte[] write = peripheral.Received.Single(pdu => pdu[0] == (byte)AttOpCode.ATT_WRITE_CMD && pdu.Length > 4);
        write[3..].ShouldBe(expected);
        controller.AclPackets.Count(packet => packet.Packet.Length > 100).ShouldBe(1);
    }

    [Fact]
    public async Task TwoLinks_TakeTurnsAtTheBuffers()
    {
        // Through the HCI layer, so both links have their frames queued when the buffers free up
        await using var controller = new FakeController();
        await using var device = new HciDevice(
            controller,
            0xE0C5AA968B6E,
            HciSettings.Default,
            TimeProvider.System,
            null
        );
        await device.InitializeAsync(Token);
        await controller.PushAsync(HciAdapter.ConnectionComplete(0x0001));
        await controller.PushAsync(HciAdapter.ConnectionComplete(0x0002));
        device.TryGetConnection(0x0001, out AclConnection? first).ShouldBeTrue();
        device.TryGetConnection(0x0002, out AclConnection? second).ShouldBeTrue();
        controller.CompletePacketsAutomatically = false;
        for (byte i = 0; i < 3; i++)
            await first.SendGattPacketAsync(Command(i), onHandOff: null, Token);
        controller.StartRecording();
        List<Task> sends = [];
        for (byte i = 0; i < 3; i++)
        {
            sends.Add(first.SendGattPacketAsync(Command(i), onHandOff: null, Token).AsTask());
            sends.Add(second.SendGattPacketAsync(Command(i), onHandOff: null, Token).AsTask());
        }

        // The controller frees its buffers in the order it got the packets: first the 3 of the first link
        for (var i = 0; i < 6; i++)
            await controller.CompletePacketsAsync(i < 3 ? (ushort)0x0001 : controller.AclPackets[i - 3].Handle);
        await Task.WhenAll(sends);

        controller.AclPackets.Select(packet => packet.Handle).ShouldBe([1, 2, 1, 2, 1, 2]);
    }

    private static AttWriteCmd Command(byte value) => new() { Handle = 0x0003, Value = new[] { value } };

    [Fact]
    public async Task AnAnswerToThePeripheral_GoesBeforeWaitingWrites()
    {
        await using var controller = new FakeController();
        (BleAdapter adapter, RemoteGattCharacteristic characteristic, _) = await ConnectAsync(controller);
        await using BleAdapter owned = adapter;
        controller.CompletePacketsAutomatically = false;
        for (byte i = 0; i < 3; i++)
            await characteristic.WriteWithoutResponseAsync(new[] { i }, Token);
        ValueTask waiting = characteristic.WriteWithoutResponseAsync(new byte[] { 4 }, Token);
        await controller.PushAsync(HciMessages.AttToHost(0x0001, new AttExchangeMtuReq { ClientRxMtu = 100 }));
        controller.StartRecording();

        await controller.CompletePacketsAsync(0x0001);
        await controller.CompletePacketsAsync(0x0001);
        await waiting;

        controller
            .AclPackets.Select(packet => packet.Packet[8])
            .ShouldBe([(byte)AttOpCode.ATT_EXCHANGE_MTU_RSP, (byte)AttOpCode.ATT_WRITE_CMD]);
    }

    [Fact]
    public async Task ALinkThatGoesDown_FailsItsWaitingWrite_AndReturnsItsBuffers()
    {
        await using var controller = new FakeController();
        (BleAdapter adapter, RemoteGattCharacteristic first, FakePeripheral peripheral) = await ConnectAsync(
            controller,
            handle: 0x0001
        );
        await using BleAdapter owned = adapter;
        (_, RemoteGattCharacteristic second, _) = await ConnectAsync(controller, adapter, handle: 0x0002);
        controller.CompletePacketsAutomatically = false;
        for (byte i = 0; i < 3; i++)
            await first.WriteWithoutResponseAsync(new[] { i }, Token);
        ValueTask waiting = first.WriteWithoutResponseAsync(new byte[] { 4 }, Token);

        await peripheral.DisconnectAsync();

        await Should.ThrowAsync<BleDisconnectedException>(waiting.AsTask());
        // The controller freed the buffers of the link that went down
        second.WriteWithoutResponseAsync(new byte[] { 5 }, Token).IsCompletedSuccessfully.ShouldBeTrue();
    }
}
