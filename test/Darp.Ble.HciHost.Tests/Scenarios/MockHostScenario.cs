using Darp.Ble.Connections;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;

namespace Darp.Ble.HciHost.Tests.Scenarios;

/// <summary> A simulated peripheral of the Mock backend hosting the service; the mock adapter is the central </summary>
internal sealed class MockHostScenario(
    BleAdapter central,
    BleAdapter host,
    ConnectedPeripheral connection,
    RemoteGattCharacteristic characteristic
) : IHostScenario
{
    private readonly BleAdapter _central = central;
    private readonly ConnectedPeripheral _connection = connection;
    private readonly RemoteGattCharacteristic _characteristic = characteristic;
    private Task<byte[]>? _started;

    public static async Task<IHostScenario> StartAsync(Action<LocalGattServiceBuilder> build)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var hosting = new TaskCompletionSource<(BleAdapter, AdvertisingSet)>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var manager = new BleManager(builder =>
            builder.AddMock(options =>
                options.AddPeripheral(
                    async (peripheral, _) =>
                    {
                        var service = new LocalGattServiceBuilder(0x180F);
                        build(service);
                        await peripheral.Peripheral.AddServiceAsync(service);
                        AdvertisingSet set = await peripheral.Broadcaster.StartAdvertisingAsync(
                            AdvertisingData.Empty,
                            TimeSpan.FromMilliseconds(100)
                        );
                        hosting.SetResult((peripheral, set));
                    }
                )
            )
        );
        BleAdapter central = await (await manager.EnumerateAdaptersAsync(token))[0].OpenAsync(token);
        (BleAdapter host, AdvertisingSet advertising) = await hosting.Task;
        ConnectedPeripheral connection = await central.Central.ConnectAsync(
            advertising.RandomAddress,
            cancellationToken: token
        );
        RemoteGattCharacteristic characteristic = (
            await (await connection.DiscoverServiceAsync(0x180F, token)).DiscoverCharacteristicsAsync(token)
        )[0];
        return new MockHostScenario(central, host, connection, characteristic);
    }

    public BleAdapter Host { get; } = host;

    public Task<byte[]> ReadAsync() => _characteristic.ReadAsync(TestContext.Current.CancellationToken);

    public Task StartReadAsync()
    {
        _started = _characteristic.ReadAsync(TestContext.Current.CancellationToken);
        return Task.CompletedTask;
    }

    public async Task<GattProtocolStatus> WriteAsync(byte[] value)
    {
        try
        {
            await _characteristic.WriteAsync(value, TestContext.Current.CancellationToken);
            return GattProtocolStatus.Success;
        }
        catch (GattException e) when (e.AttStatus is { } status)
        {
            return status;
        }
    }

    public Task DisconnectAsync() => _connection.DisconnectAsync(TestContext.Current.CancellationToken);

    public async Task<bool> GotAnswerAsync()
    {
        if (_started is null)
            return false;
        try
        {
            await _started;
            return true;
        }
        catch (GattException)
        {
            return true;
        }
        catch (BleDisconnectedException)
        {
            return false;
        }
    }

    public ValueTask DisposeAsync() => _central.DisposeAsync();
}
