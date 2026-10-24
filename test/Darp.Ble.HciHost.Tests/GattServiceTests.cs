using Darp.Ble.Connections;
using Darp.Ble.Gatt;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

/// <summary> The GATT service HciHost serves after GAP, as a fake central sees it </summary>
public sealed class GattServiceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    // GAP: service 1, device name 2-3, appearance 4-5. GATT: service 6, Service Changed 7-8 with its CCCD 9,
    // Database Hash 10-11
    private const byte ServiceChanged = 0x08;
    private const byte ServiceChangedCccd = 0x09;
    private const byte DatabaseHash = 0x0B;

    [Fact]
    public async Task ACentral_ThatTurnedIndicationsOn_LearnsOfEveryAddedService_OneIndicationAtATime()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        byte[] notifications = await central.RequestAsync(0x12, ServiceChangedCccd, 0x00, 0x01, 0x00);
        byte[] indications = await central.RequestAsync(0x12, ServiceChangedCccd, 0x00, 0x02, 0x00);
        await adapter.Peripheral.AddServiceAsync(new LocalGattServiceBuilder(0x180F), Token);
        await adapter.Peripheral.AddServiceAsync(new LocalGattServiceBuilder(0x180A), Token);
        byte[] first = await central.NextAnswerAsync();
        bool secondBeforeConfirmation = central.HasPending;
        await central.SendAsync(0x1E);
        byte[] second = await central.NextAnswerAsync();

        // Service Changed only indicates
        notifications.ShouldBe([0x01, 0x12, ServiceChangedCccd, 0x00, 0xFD]);
        indications.ShouldBe([0x13]);
        // Handle Value Indication over every handle
        first.ShouldBe([0x1D, ServiceChanged, 0x00, 0x01, 0x00, 0xFF, 0xFF]);
        secondBeforeConfirmation.ShouldBeFalse();
        second.ShouldBe(first);
    }

    [Fact]
    public async Task ACentral_ThatTurnsIndicationsOffBeforeConfirming_GetsNoFurtherIndication()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        await central.RequestAsync(0x12, ServiceChangedCccd, 0x00, 0x02, 0x00);
        await adapter.Peripheral.AddServiceAsync(new LocalGattServiceBuilder(0x180F), Token);
        await adapter.Peripheral.AddServiceAsync(new LocalGattServiceBuilder(0x180A), Token);
        await central.NextAnswerAsync();
        await central.RequestAsync(0x12, ServiceChangedCccd, 0x00, 0x00, 0x00);
        await central.SendAsync(0x1E);
        // A request after the confirmation: its answer comes after anything the confirmation would have sent
        byte[] answer = await central.RequestAsync(0x0A, ServiceChangedCccd, 0x00);

        answer.ShouldBe([0x0B, 0x00, 0x00]);
        central.HasPending.ShouldBeFalse();
    }

    [Fact]
    public async Task ACentral_ThatNeverConfirms_IsDisconnectedAfterTheAttTimeout()
    {
        var time = new FakeTimeProvider();
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller, time);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();
        while (adapter.Peripheral.ConnectedCentrals.Count == 0)
            await Task.Delay(5, Token);
        ConnectedCentral connected = adapter.Peripheral.ConnectedCentrals[0];

        await central.RequestAsync(0x12, ServiceChangedCccd, 0x00, 0x02, 0x00);
        await adapter.Peripheral.AddServiceAsync(new LocalGattServiceBuilder(0x180F), Token);
        await central.NextAnswerAsync();
        time.Advance(TimeSpan.FromSeconds(29));
        bool connectedBefore = !connected.Disconnected.IsCompleted;
        time.Advance(TimeSpan.FromSeconds(1));

        connectedBefore.ShouldBeTrue();
        (await connected.Disconnected.WaitAsync(TimeSpan.FromSeconds(5), Token)).Reason.ShouldBe(
            DisconnectReason.AttTimeout
        );
        controller.Commands.ShouldContain(Hci.Package.HciOpCode.HCI_Disconnect);
    }

    [Fact]
    public async Task AddingAnotherGattService_Throws()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            adapter.Peripheral.AddServiceAsync(new LocalGattServiceBuilder(0x1801), Token)
        );
    }

    [Fact]
    public async Task AddingAnExtendedPropertiesDescriptor_Throws()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        var service = new LocalGattServiceBuilder(0x180F);
        service.AddCharacteristic(0x2A19, GattProperty.Read).AddDescriptor(0x2900);

        await Should.ThrowAsync<NotSupportedException>(() => adapter.Peripheral.AddServiceAsync(service, Token));
    }

    [Fact]
    public async Task TheDatabaseHash_IsTheAesCmacOfTheDeclarationsAndDescriptors()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        byte[] answer = await central.RequestAsync(0x0A, DatabaseHash, 0x00);

        // AES-CMAC with a zero key over the handle, type and value of each declaration and the handle and type of the
        // CCCD, in handle order (Core 6.0, Vol 3, Part G, 7.3):
        // 010000280018 02000328020300002A 04000328020500012A 060000280118 07000328200800052A 09000229 0A000328020B002A2B
        // computed separately, least significant byte first
        answer.ShouldBe([0x0B, .. Convert.FromHexString("3BF4CA3F1A967566745F943BD3CB5523")]);
    }

    [Fact]
    public async Task AnAdapterWithoutTheGattService_ServesOnlyGap()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(
            controller,
            configure: options => options.IncludeGattService = false
        );
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();

        byte[] services = await central.RequestAsync(0x10, 0x01, 0x00, 0xFF, 0xFF, 0x00, 0x28);

        Convert.ToHexString(services).ShouldBe("1106" + "010005000018");
    }
}
