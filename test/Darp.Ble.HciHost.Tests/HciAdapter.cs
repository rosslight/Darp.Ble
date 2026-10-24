using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Time.Testing;

namespace Darp.Ble.HciHost.Tests;

/// <summary> Opens an HCI adapter over a fake controller, through the public API </summary>
internal static class HciAdapter
{
    public static readonly BleAddress Address = BleAddress.CreateRandomAddress((UInt48)0xE0C5AA968B6E);
    public static readonly BleAddress Peer = BleAddress.CreateRandomAddress((UInt48)0x112233445566);

    public static BleManager Manager(
        FakeController controller,
        TimeProvider? time = null,
        Action<HciHostOptions>? configure = null
    ) =>
        new(
            builder =>
                builder.AddHciHost(
                    controller,
                    options =>
                    {
                        options.Name = "Fake controller";
                        options.RandomAddress = Address;
                        configure?.Invoke(options);
                    }
                ),
            timeProvider: time ?? new FakeTimeProvider()
        );

    public static async Task<BleAdapter> OpenAsync(
        FakeController controller,
        TimeProvider? time = null,
        Action<HciHostOptions>? configure = null
    )
    {
        IReadOnlyList<BleAdapterInfo> adapters = await Manager(controller, time, configure)
            .EnumerateAdaptersAsync(TestContext.Current.CancellationToken);
        BleAdapter adapter = await adapters[0].OpenAsync(TestContext.Current.CancellationToken);
        controller.StartRecording();
        return adapter;
    }

    /// <summary> The controller reports the link as up </summary>
    public static HciMessage ConnectionComplete(
        ushort handle,
        HciCommandStatus status = HciCommandStatus.Success,
        HciLeConnectionRole role = HciLeConnectionRole.Central,
        BleAddress? peer = null
    ) =>
        HciMessages.HciLeEnhancedConnectionCompleteEvent(
            handle,
            role,
            (byte)(peer ?? Peer).Type,
            (peer ?? Peer).Value,
            connectionInterval: 0x0050,
            peripheralLatency: 0x0000,
            supervisionTimeout: 0x0048,
            status: status
        );

    /// <summary> Connects to <see cref="Peer"/>; the peripheral agrees on the given MTU </summary>
    public static async Task<ConnectedPeripheral> ConnectAsync(
        BleAdapter adapter,
        FakeController controller,
        ushort handle = 0x0001,
        ushort serverMtu = 65
    )
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        Task<ConnectedPeripheral> connecting = adapter.Central.ConnectAsync(Peer, cancellationToken: token);
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Extended_Create_Connection_V1);
        await controller.PushAsync(ConnectionComplete(handle));
        await controller.ReadAclAsync(token);
        await controller.PushAsync(HciMessages.AttExchangeMtuResponse(handle, serverMtu));
        return await connecting;
    }

    /// <summary> Connects to <see cref="Peer"/> and lets the peripheral answer from then on </summary>
    public static async Task<ConnectedPeripheral> ConnectAsync(
        BleAdapter adapter,
        FakeController controller,
        FakePeripheral peripheral
    )
    {
        _ = FakePeripheral.ServeAsync(controller, peripheral);
        Task<ConnectedPeripheral> connecting = adapter.Central.ConnectAsync(
            Peer,
            cancellationToken: TestContext.Current.CancellationToken
        );
        await controller.WaitForCommandAsync(HciOpCode.HCI_LE_Extended_Create_Connection_V1);
        await controller.PushAsync(ConnectionComplete(peripheral.Handle));
        return await connecting;
    }
}
