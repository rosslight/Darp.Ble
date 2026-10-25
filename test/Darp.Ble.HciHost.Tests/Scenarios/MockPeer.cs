using System.Threading.Channels;
using Darp.Ble.Connections;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Darp.Ble.Mock;
using Darp.Ble.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Darp.Ble.HciHost.Tests.Scenarios;

/// <summary> The peripheral as a simulated peripheral of the Mock backend </summary>
internal sealed class MockPeer : IScenarioPeer
{
    private readonly BleAdapter _adapter;
    private readonly State _state;

    private MockPeer(
        BleAdapter adapter,
        State state,
        FakeTimeProvider time,
        ConnectedPeripheral connection,
        RemoteGattCharacteristic characteristic
    )
    {
        _adapter = adapter;
        _state = state;
        Time = time;
        Connection = connection;
        Characteristic = characteristic;
    }

    public static async Task<IScenarioPeer> ConnectAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var time = new FakeTimeProvider();
        var state = new State();
        var advertising = new TaskCompletionSource<AdvertisingSet>(TaskCreationOptions.RunContinuationsAsynchronously);
        var manager = new BleManager(
            builder =>
                builder.AddMock(options =>
                    options.AddPeripheral(
                        async (peripheral, _) =>
                        {
                            state.Peripheral = peripheral;
                            var service = new LocalGattServiceBuilder(0x180F);
                            service.AddCharacteristic(
                                0xFF01,
                                GattProperty.Read | GattProperty.Write,
                                onRead: async (_, cancellationToken) =>
                                {
                                    await state.OnRequestAsync("read", cancellationToken);
                                    return state.Value;
                                },
                                onWrite: async (_, value, cancellationToken) =>
                                {
                                    await state.OnRequestAsync(
                                        $"write {Convert.ToHexString(value.Span)}",
                                        cancellationToken
                                    );
                                    return GattProtocolStatus.Success;
                                }
                            );
                            await peripheral.Peripheral.AddServiceAsync(service);
                            advertising.SetResult(
                                await peripheral.Broadcaster.StartAdvertisingAsync(
                                    AdvertisingData.Empty,
                                    TimeSpan.FromMilliseconds(100)
                                )
                            );
                        }
                    )
                ),
            timeProvider: time
        );
        BleAdapter adapter = await (await manager.EnumerateAdaptersAsync(token))[0].OpenAsync(token);
        AdvertisingSet set = await advertising.Task;
        ConnectedPeripheral connection = await adapter.Central.ConnectAsync(
            set.RandomAddress,
            cancellationToken: token
        );
        RemoteGattCharacteristic characteristic = (
            await (await connection.DiscoverServiceAsync(0x180F, token)).DiscoverCharacteristicsAsync(token)
        )[0];
        return new MockPeer(adapter, state, time, connection, characteristic);
    }

    public FakeTimeProvider Time { get; }

    public ConnectedPeripheral Connection { get; }

    public RemoteGattCharacteristic Characteristic { get; }

    public bool HoldAnswers
    {
        get => _state.Hold;
        set => _state.Hold = value;
    }

    public IReadOnlyList<string> Requests
    {
        get
        {
            lock (_state)
                return [.. _state.Traffic.Where(entry => entry is not Answer)];
        }
    }

    public IReadOnlyList<string> Traffic
    {
        get
        {
            lock (_state)
                return [.. _state.Traffic];
        }
    }

    public Task WaitForRequestsAsync(int count) => WaitForAsync(count).WithWatchdog($"{count} requests");

    private async Task WaitForAsync(int count)
    {
        while (Requests.Count < count)
            await _state.Changed.Reader.ReadAsync(TestContext.Current.CancellationToken);
    }

    public Task ReleaseAnswerAsync()
    {
        TaskCompletionSource held;
        lock (_state)
        {
            held = _state.Held.Dequeue();
            _state.Traffic.Add(Answer);
        }
        held.SetResult();
        return Task.CompletedTask;
    }

    public void SetValue(byte[] value) => _state.Value = value;

    public Task DisconnectAsync() =>
        _state.Peripheral?.Peripheral.ConnectedCentrals.Single().DisconnectAsync(TestContext.Current.CancellationToken)
        ?? Task.CompletedTask;

    public ValueTask DisposeAsync() => _adapter.DisposeAsync();

    private const string Answer = "answer";

    private sealed class State
    {
        public BleAdapter? Peripheral { get; set; }
        public bool Hold { get; set; }
        public byte[] Value { get; set; } = [0];
        public List<string> Traffic { get; } = [];
        public Queue<TaskCompletionSource> Held { get; } = new();
        public Channel<bool> Changed { get; } = Channel.CreateUnbounded<bool>();

        /// <summary> Records the request; a held answer waits for its release or for the end of the connection </summary>
        public Task OnRequestAsync(string request, CancellationToken cancellationToken)
        {
            Task answer = Task.CompletedTask;
            lock (this)
            {
                Traffic.Add(request);
                if (Hold)
                {
                    var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    Held.Enqueue(held);
                    answer = held.Task.WaitAsync(cancellationToken);
                }
                else
                {
                    Traffic.Add(Answer);
                }
            }
            Changed.Writer.TryWrite(true);
            return answer;
        }
    }
}
