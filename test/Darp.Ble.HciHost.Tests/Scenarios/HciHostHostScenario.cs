using Darp.Ble.Gatt;
using Darp.Ble.HciHost.Verify;

namespace Darp.Ble.HciHost.Tests.Scenarios;

/// <summary> An HCI adapter hosting the service; a fake central behind the fake controller </summary>
internal sealed class HciHostHostScenario(FakeController controller, BleAdapter host, FakeCentral central)
    : IHostScenario
{
    // GAP takes handles 1-5 and GATT 6-11; the service 12, its characteristic's declaration 13 and value 14
    private const byte Value = 0x0E;

    private readonly FakeController _controller = controller;
    private readonly FakeCentral _central = central;

    public static async Task<IHostScenario> StartAsync(Action<LocalGattServiceBuilder> build)
    {
        var controller = new FakeController();
        BleAdapter host = await HciAdapter.OpenAsync(controller);
        var service = new LocalGattServiceBuilder(0x180F);
        build(service);
        await host.Peripheral.AddServiceAsync(service, TestContext.Current.CancellationToken);
        var central = new FakeCentral(controller, 0x0001);
        await central.ConnectAsync();
        return new HciHostHostScenario(controller, host, central);
    }

    public BleAdapter Host { get; } = host;

    public async Task<byte[]> ReadAsync() => (await _central.RequestAsync(0x0A, Value, 0x00))[1..];

    public Task StartReadAsync() => _central.SendAsync(0x0A, Value, 0x00);

    public async Task<GattProtocolStatus> WriteAsync(byte[] value)
    {
        byte[] answer = await _central.RequestAsync([0x12, Value, 0x00, .. value]);
        return answer[0] == 0x13 ? GattProtocolStatus.Success : (GattProtocolStatus)answer[^1];
    }

    public Task DisconnectAsync() => _central.DisconnectAsync();

    public async Task<bool> GotAnswerAsync()
    {
        // The host answers on its own threads; give it the time an answer takes
        await Task.Delay(100, TestContext.Current.CancellationToken);
        return _central.HasPending;
    }

    public async ValueTask DisposeAsync()
    {
        await Host.DisposeAsync();
        await ((IAsyncDisposable)_controller).DisposeAsync();
    }
}
