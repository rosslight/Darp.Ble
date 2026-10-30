using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gatt;
using Darp.Ble.HciHost.Verify;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

/// <summary> Hosting services on an HCI adapter for centrals </summary>
public sealed class PeripheralTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    // GAP: service 1, device name 2-3, appearance 4-5. GATT: service 6, Service Changed 7-8 with its CCCD 9,
    // Database Hash 10-11. The battery service follows
    private const byte Battery = 0x0C;
    private const byte Level = 0x0E;
    private const byte LevelCccd = 0x0F;

    // A second characteristic for writes: declaration 16, value 17
    private const byte Control = 0x11;

    private static LocalGattCharacteristic? _level;

    private static async Task<BleAdapter> HostAsync(
        FakeController controller,
        OnReadHandler? onRead = null,
        OnWriteHandler? onWrite = null
    )
    {
        BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        var service = new LocalGattServiceBuilder(0x180F);
        _level = service.AddCharacteristic(
            0x2A19,
            GattProperty.Read | GattProperty.Notify,
            onRead: onRead ?? ((_, _) => ValueTask.FromResult<byte[]>([80]))
        );
        service.AddCharacteristic(
            0xFF01,
            GattProperty.Write | GattProperty.WriteWithoutResponse,
            onWrite: onWrite ?? ((_, _, _) => ValueTask.FromResult(GattProtocolStatus.Success))
        );
        await adapter.Peripheral.AddServiceAsync(service, Token);
        controller.StartRecording();
        return adapter;
    }

    [Fact]
    public async Task ACentral_DiscoversTheGapAndGattServicesFirst_AndThenTheHostedServices()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        // Read By Group Type of primary services, then the characteristics and descriptors of the battery service
        byte[] services = await central.RequestAsync(0x10, 0x01, 0x00, 0xFF, 0xFF, 0x00, 0x28);
        byte[] characteristics = await central.RequestAsync(0x08, Battery, 0x00, 0xFF, 0xFF, 0x03, 0x28);
        byte[] descriptors = await central.RequestAsync(0x04, LevelCccd, 0x00, LevelCccd, 0x00);

        Convert.ToHexString(services).ShouldBe("1106" + "010005000018" + "06000B000118" + "0C0011000F18");
        Convert.ToHexString(characteristics).ShouldBe("0907" + "0D00120E00192A" + "10000C110001FF");
        Convert.ToHexString(descriptors).ShouldBe("0501" + "0F00" + "0229");
    }

    [Fact]
    public async Task AReadOfAHostedCharacteristic_IsAnsweredByTheAppsHandler()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        byte[] answer = await central.RequestAsync(0x0A, Level, 0x00);

        answer.ShouldBe([0x0B, 80]);
    }

    [Fact]
    public async Task TheGapService_ServesTheDeviceNameAndAppearance()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        adapter.Peripheral.DeviceName = "Sensor";
        adapter.Peripheral.Appearance = AppearanceValues.Phone;
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        byte[] name = await central.RequestAsync(0x0A, 0x03, 0x00);
        byte[] appearance = await central.RequestAsync(0x0A, 0x05, 0x00);

        name.ShouldBe([0x0B, .. "Sensor"u8.ToArray()]);
        appearance.ShouldBe([0x0B, (byte)AppearanceValues.Phone, (byte)((ushort)AppearanceValues.Phone >> 8)]);
    }

    [Fact]
    public async Task AWriteRequest_ReachesTheAppsHandler_AndItsStatusIsTheAnswer()
    {
        await using var controller = new FakeController();
        List<byte[]> written = [];
        await using BleAdapter adapter = await HostAsync(
            controller,
            onWrite: (_, value, _) =>
            {
                written.Add(value.ToArray());
                return ValueTask.FromResult(
                    value.Span[0] == 0xFF ? GattProtocolStatus.WriteRequestRejected : GattProtocolStatus.Success
                );
            }
        );
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        byte[] accepted = await central.RequestAsync(0x12, Control, 0x00, 0x01);
        byte[] rejected = await central.RequestAsync(0x12, Control, 0x00, 0xFF);

        accepted.ShouldBe([0x13]);
        rejected.ShouldBe([0x01, 0x12, Control, 0x00, (byte)GattProtocolStatus.WriteRequestRejected]);
        written.Select(Convert.ToHexString).ShouldBe(["01", "FF"]);
    }

    [Fact]
    public async Task AWriteCommand_ReachesTheAppsHandler_WithoutAnAnswer()
    {
        await using var controller = new FakeController();
        var written = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using BleAdapter adapter = await HostAsync(
            controller,
            onWrite: (_, value, _) =>
            {
                written.TrySetResult(value.ToArray());
                return ValueTask.FromResult(GattProtocolStatus.Success);
            }
        );
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        await central.SendAsync(0x52, Control, 0x00, 0x07);

        (await written.Task.WaitAsync(Token)).ShouldBe([0x07]);
        // A read afterwards is the next thing the central gets
        (await central.RequestAsync(0x0A, Level, 0x00)).ShouldBe([0x0B, 80]);
    }

    [Fact]
    public async Task WritingAReadOnlyCharacteristic_IsNotPermitted()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        byte[] answer = await central.RequestAsync(0x12, Level, 0x00, 0x01);

        answer.ShouldBe([0x01, 0x12, Level, 0x00, 0x03]);
    }

    [Fact]
    public async Task AnUnsupportedRequest_IsAnsweredAsNotSupported()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        // Prepare Write: long writes are not supported
        byte[] answer = await central.RequestAsync(0x16, Control, 0x00, 0x00, 0x00, 0x01);

        answer[0].ShouldBe((byte)0x01);
        answer[1].ShouldBe((byte)0x16);
        answer[^1].ShouldBe((byte)0x06);
    }

    [Fact]
    public async Task AValueLongerThanOneAnswer_IsReadInPartsWithReadBlob()
    {
        await using var controller = new FakeController();
        byte[] value = [.. Enumerable.Range(0, 40).Select(i => (byte)i)];
        var reads = 0;
        await using BleAdapter adapter = await HostAsync(
            controller,
            onRead: (_, _) =>
            {
                reads++;
                return ValueTask.FromResult(value);
            }
        );
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        // At the default MTU an answer carries 22 bytes; the parts end with an empty one where the value ends
        byte[] first = await central.RequestAsync(0x0A, Level, 0x00);
        byte[] second = await central.RequestAsync(0x0C, Level, 0x00, 22, 0x00);
        byte[] end = await central.RequestAsync(0x0C, Level, 0x00, 40, 0x00);
        byte[] beyond = await central.RequestAsync(0x0C, Level, 0x00, 41, 0x00);

        first.ShouldBe([0x0B, .. value[..22]]);
        second.ShouldBe([0x0D, .. value[22..]]);
        end.ShouldBe([0x0D]);
        beyond.ShouldBe([0x01, 0x0C, Level, 0x00, 0x07]);
        // The parts come from one answer of the app, so they belong to the same value
        reads.ShouldBe(1);
    }

    [Fact]
    public async Task ANewRead_AsksTheAppAgain_OnceTheLongReadBeforeItEnded()
    {
        await using var controller = new FakeController();
        byte[] value = [.. Enumerable.Repeat((byte)1, 40)];
        await using BleAdapter adapter = await HostAsync(controller, onRead: (_, _) => ValueTask.FromResult(value));
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();
        await central.RequestAsync(0x0A, Level, 0x00);
        await central.RequestAsync(0x0C, Level, 0x00, 22, 0x00);
        value = [.. Enumerable.Repeat((byte)2, 40)];

        // Read Blob at offset 0 starts a new read
        byte[] answer = await central.RequestAsync(0x0C, Level, 0x00, 0, 0x00);

        answer.ShouldBe([0x0D, .. value[..22]]);
    }

    [Theory]
    [InlineData(new byte[] { 0x0A, Level, 0x00 }, 22)]
    [InlineData(new byte[] { 0x0C, Level, 0x00, 0, 0x00 }, 22)]
    [InlineData(new byte[] { 0x08, Level, 0x00, Level, 0x00, 0x19, 0x2A }, 19)]
    public async Task AValueThatExactlyFillsTheFirstPart_EndsWithTheSameValue(byte[] firstRequest, byte partLength)
    {
        await using var controller = new FakeController();
        byte[] value = [.. Enumerable.Repeat((byte)1, partLength)];
        var reads = 0;
        await using BleAdapter adapter = await HostAsync(
            controller,
            onRead: (_, _) =>
            {
                reads++;
                return ValueTask.FromResult(value);
            }
        );
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();
        await central.RequestAsync(firstRequest);
        value = [.. Enumerable.Repeat((byte)2, 44)];

        // A full part may not be the last, so the central asks where it ended
        byte[] end = await central.RequestAsync(0x0C, Level, 0x00, partLength, 0x00);

        end.ShouldBe([0x0D]);
        reads.ShouldBe(1);
    }

    [Fact]
    public async Task ReadBlob_ContinuesTheValueThatReadByTypeStartedWith()
    {
        await using var controller = new FakeController();
        byte[] value = [.. Enumerable.Repeat((byte)1, 40)];
        await using BleAdapter adapter = await HostAsync(controller, onRead: (_, _) => ValueTask.FromResult(value));
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();
        await central.RequestAsync(0x0A, Level, 0x00);
        value = [.. Enumerable.Repeat((byte)2, 40)];

        // Read By Type answers MTU - 4 = 19 bytes of the value
        byte[] byType = await central.RequestAsync(0x08, Level, 0x00, Level, 0x00, 0x19, 0x2A);
        value = [.. Enumerable.Repeat((byte)3, 40)];
        byte[] rest = await central.RequestAsync(0x0C, Level, 0x00, 19, 0x00);

        byType[4].ShouldBe((byte)2);
        rest.ShouldBe([0x0D, .. Enumerable.Repeat((byte)2, 21)]);
    }

    [Fact]
    public async Task ALongReadThatStartsWithReadBlob_AsksTheAppOnce()
    {
        await using var controller = new FakeController();
        var reads = 0;
        await using BleAdapter adapter = await HostAsync(
            controller,
            onRead: (_, _) => ValueTask.FromResult<byte[]>([.. Enumerable.Repeat((byte)++reads, 40)])
        );
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        byte[] first = await central.RequestAsync(0x0C, Level, 0x00, 0, 0x00);
        byte[] second = await central.RequestAsync(0x0C, Level, 0x00, 22, 0x00);

        first[1].ShouldBe((byte)1);
        second[1].ShouldBe((byte)1);
        reads.ShouldBe(1);
    }

    [Fact]
    public async Task AWrite_EndsALongRead_SoTheNextPartComesFromTheNewValue()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        byte[] value = [.. Enumerable.Repeat((byte)1, 40)];
        var service = new LocalGattServiceBuilder(0x180F);
        service.AddCharacteristic(
            0xFF02,
            GattProperty.Read | GattProperty.Write,
            onRead: (_, _) => ValueTask.FromResult(value),
            onWrite: (_, written, _) =>
            {
                value = [.. Enumerable.Repeat(written.Span[0], 40)];
                return ValueTask.FromResult(GattProtocolStatus.Success);
            }
        );
        await adapter.Peripheral.AddServiceAsync(service, Token);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();
        // The service's characteristic value follows the GAP and GATT services, as for the battery level
        await central.RequestAsync(0x0A, Level, 0x00);

        await central.RequestAsync(0x12, Level, 0x00, 7);
        byte[] rest = await central.RequestAsync(0x0C, Level, 0x00, 22, 0x00);

        rest.ShouldBe([0x0D, .. Enumerable.Repeat((byte)7, 18)]);
    }

    [Fact]
    public async Task ACentralThatTurnsNotificationsOn_GetsThem_AndOthersDoNot()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        var subscribed = new FakeCentral(controller, 0x0001);
        var other = new FakeCentral(controller, 0x0002);
        await subscribed.ConnectAsync();
        await other.ConnectAsync();

        (await subscribed.RequestAsync(0x12, LevelCccd, 0x00, 0x01, 0x00)).ShouldBe([0x13]);
        // The core learned of the subscription before the write was answered
        await _level!.NotifyAllAsync(new byte[] { 79 }, Token);

        (ushort handle, byte[] value) = await subscribed.NextNotificationAsync();
        handle.ShouldBe(Level);
        value.ShouldBe([79]);
        (await other.RequestAsync(0x0A, LevelCccd, 0x00)).ShouldBe([0x0B, 0x00, 0x00]);
        other.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task ACentralThatDisconnects_EndsItsConnection()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();
        ConnectedCentral connected = await WaitForCentralAsync(adapter);

        await central.DisconnectAsync();

        (await connected.Disconnected).Reason.ShouldBe(DisconnectReason.RemoteRequest);
        adapter.Peripheral.ConnectedCentrals.ShouldBeEmpty();
    }

    [Fact]
    public async Task DisconnectingACentral_TakesTheLinkDown()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();
        ConnectedCentral connected = await WaitForCentralAsync(adapter);

        await connected.DisconnectAsync(Token);

        (await connected.Disconnected).Reason.ShouldBe(DisconnectReason.LocalRequest);
        controller.Commands.ShouldContain(Hci.Package.HciOpCode.HCI_Disconnect);
    }

    [Fact]
    public async Task ACentralThatDidNotTurnNotificationsOn_GetsNoneSentToItDirectly()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();
        ConnectedCentral connected = await WaitForCentralAsync(adapter);

        await _level!.NotifyAsync(connected, new byte[] { 1 }, Token);
        (await central.RequestAsync(0x12, LevelCccd, 0x00, 0x01, 0x00)).ShouldBe([0x13]);
        await _level.NotifyAsync(connected, new byte[] { 2 }, Token);
        (await central.RequestAsync(0x12, LevelCccd, 0x00, 0x00, 0x00)).ShouldBe([0x13]);
        await _level.NotifyAsync(connected, new byte[] { 3 }, Token);
        // A read shows whatever came before it
        await central.RequestAsync(0x0A, Level, 0x00);

        central.Notifications.Select(n => n.Value[0]).ShouldBe([(byte)2]);
    }

    [Fact]
    public async Task ACccdValueTheCharacteristicDoesNotSupport_IsRejected_AndLeavesTheCccdAsItWas()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        // Indications on a characteristic that only notifies
        byte[] answer = await central.RequestAsync(0x12, LevelCccd, 0x00, 0x02, 0x00);

        answer.ShouldBe([0x01, 0x12, LevelCccd, 0x00, 0xFD]);
        (await central.RequestAsync(0x0A, LevelCccd, 0x00)).ShouldBe([0x0B, 0x00, 0x00]);
    }

    [Fact]
    public async Task ATypeIn128Bits_FindsTheSameAttributes()
    {
        await using var controller = new FakeController();
        BleUuid custom = BleUuid.Parse("6E400001-B5A3-F393-E0A9-E50E24DCCA9E", null);
        BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using BleAdapter owned = adapter;
        var service = new LocalGattServiceBuilder(0x180F);
        service.AddCharacteristic(custom, GattProperty.Read, onRead: (_, _) => ValueTask.FromResult<byte[]>([42]));
        await adapter.Peripheral.AddServiceAsync(service, Token);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        byte[] byCustomType = await central.RequestAsync([0x08, 0x01, 0x00, 0xFF, 0xFF, .. custom.ToByteArray()]);
        // The characteristic declaration type 0x2803 in its 128-bit form
        byte[] declarationType = Convert.FromHexString("FB349B5F80000080001000000328" + "0000");
        byte[] byLongDeclarationType = await central.RequestAsync([
            0x08,
            Battery,
            0x00,
            0xFF,
            0xFF,
            .. declarationType,
        ]);

        byCustomType.ShouldBe([0x09, 0x03, 0x0E, 0x00, 42]);
        byLongDeclarationType[0].ShouldBe((byte)0x09);
    }

    [Fact]
    public async Task A32BitUuid_IsSentIn128Bits()
    {
        await using var controller = new FakeController();
        BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        await using BleAdapter owned = adapter;
        var service = new LocalGattServiceBuilder(BleUuid.FromUInt32(0x12345678));
        service.AddCharacteristic(
            BleUuid.FromUInt32(0x12345679),
            GattProperty.Read,
            onRead: (_, _) => ValueTask.FromResult<byte[]>([1])
        );
        await adapter.Peripheral.AddServiceAsync(service, Token);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        byte[] services = await central.RequestAsync(0x10, Battery, 0x00, 0xFF, 0xFF, 0x00, 0x28);
        byte[] characteristics = await central.RequestAsync(0x08, Battery, 0x00, 0xFF, 0xFF, 0x03, 0x28);
        byte[] information = await central.RequestAsync(0x04, 0x0E, 0x00, 0x0E, 0x00);

        services[..2].ShouldBe([0x11, 4 + 16]);
        BleUuid.Read(services.AsSpan(6, 16)).Value.ShouldBe(BleUuid.FromUInt32(0x12345678).Value);
        characteristics[..2].ShouldBe([0x09, 2 + 3 + 16]);
        information[..2].ShouldBe([0x05, 0x02]);
        BleUuid.Read(information.AsSpan(4, 16)).Value.ShouldBe(BleUuid.FromUInt32(0x12345679).Value);
    }

    [Fact]
    public async Task FindByTypeValue_FindsAValueTheAppAnswersFor()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        // Battery level 0x2A19 with the value 80
        byte[] answer = await central.RequestAsync(0x06, 0x01, 0x00, 0xFF, 0xFF, 0x19, 0x2A, 80);

        answer.ShouldBe([0x07, Level, 0x00, Level, 0x00]);
    }

    [Fact]
    public async Task FindByTypeValue_FindsAServiceByItsUuidInEitherWidth()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();
        // Primary service 0x2800 with the value Battery 0x180F, as 16 bits and as the 128-bit Bluetooth base UUID
        byte[] battery128 =
        [
            0xFB,
            0x34,
            0x9B,
            0x5F,
            0x80,
            0x00,
            0x00,
            0x80,
            0x00,
            0x10,
            0x00,
            0x00,
            0x0F,
            0x18,
            0x00,
            0x00,
        ];

        byte[] by16 = await central.RequestAsync(0x06, 0x01, 0x00, 0xFF, 0xFF, 0x00, 0x28, 0x0F, 0x18);
        byte[] by128 = await central.RequestAsync([0x06, 0x01, 0x00, 0xFF, 0xFF, 0x00, 0x28, .. battery128]);

        by16[0].ShouldBe((byte)0x07);
        by128.ShouldBe(by16);
    }

    [Theory]
    [InlineData(0x10)]
    [InlineData(0x08)]
    [InlineData(0x06)]
    public async Task ADiscoveryStartingAtHandleZero_IsAnInvalidHandle(byte opCode)
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();
        byte[] request =
            opCode == 0x06
                ? [opCode, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x28, 0x0F, 0x18]
                : [opCode, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x28];

        byte[] answer = await central.RequestAsync(request);

        answer.ShouldBe([0x01, opCode, 0x00, 0x00, 0x01]);
    }

    [Fact]
    public async Task ReadMultipleVariable_IsAnsweredAsNotSupported()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        byte[] answer = await central.RequestAsync(0x20, Level, 0x00, LevelCccd, 0x00);

        answer.ShouldBe([0x01, 0x20, 0x00, 0x00, 0x06]);
    }

    [Fact]
    public async Task ACentralThatConnectsAfterTheTransportFailed_IsNotAdmitted()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HostAsync(controller);
        controller.Fail(new IOException("The serial port is gone"));

        await new FakeCentral(controller, 0x0001).ConnectAsync();

        adapter.Peripheral.ConnectedCentrals.ShouldBeEmpty();
    }

    private static async Task<ConnectedCentral> WaitForCentralAsync(BleAdapter adapter)
    {
        await WaitUntilAsync(() => adapter.Peripheral.ConnectedCentrals.Count == 1);
        return adapter.Peripheral.ConnectedCentrals[0];
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        while (!condition())
            await Task.Delay(5, Token);
    }
}
