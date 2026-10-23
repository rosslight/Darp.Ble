using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Tests.Connections;

/// <summary> A peripheral role the test drives: it connects centrals and decides when each notification is accepted </summary>
internal sealed class TestPeripheralDriver : IPeripheralDriver
{
    private readonly Lock _lock = new();
    private readonly Dictionary<ICentralConnectionDriver, TaskCompletionSource> _holds = [];
    private IGattServerSink? _sink;

    public IGattServerSink Sink => _sink ?? throw new InvalidOperationException("Not attached");

    /// <summary> Notifications the backend accepted, in order, with the central they went to </summary>
    public List<(BleAddress Central, byte[] Value)> Accepted { get; } = [];

    /// <summary> Centrals whose notifications fail </summary>
    public HashSet<ICentralConnectionDriver> Failing { get; } = [];

    public GattServiceDefinition? Added { get; private set; }

    public void Attach(IGattServerSink sink) => _sink = sink;

    public Task AddServiceAsync(GattServiceDefinition service, CancellationToken cancellationToken)
    {
        Added = service;
        return Task.CompletedTask;
    }

    /// <summary> Connects a central and turns its notifications of the added characteristic on </summary>
    public TestCentralConnection ConnectSubscribed(byte address)
    {
        var central = new TestCentralConnection(new BleAddress(BleAddressType.Public, (UInt48)address));
        Sink.OnCentralConnected(central);
        Sink.OnSubscriptionChanged(central, AddedCharacteristic, enabled: true);
        return central;
    }

    public GattCharacteristicDefinition AddedCharacteristic =>
        Added?.Characteristics.Single() ?? throw new InvalidOperationException("Nothing added");

    /// <summary> Holds the notifications of a central until the test releases them </summary>
    public TaskCompletionSource Hold(ICentralConnectionDriver central)
    {
        var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lock)
            _holds[central] = hold;
        return hold;
    }

    public async ValueTask NotifyAsync(
        ICentralConnectionDriver central,
        GattCharacteristicDefinition characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        TaskCompletionSource? hold;
        lock (_lock)
            _holds.TryGetValue(central, out hold);
        // Waiting for buffer space honours the token
        if (hold is not null)
            await hold.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (Failing.Contains(central))
            throw new IOException("The central's link failed");
        lock (_lock)
            Accepted.Add((central.Address, value.ToArray()));
    }

    public Task DisconnectAsync(ICentralConnectionDriver central, CancellationToken cancellationToken)
    {
        Sink.OnCentralDisconnected(central, new DisconnectInfo(DisconnectReason.LocalRequest));
        return Task.CompletedTask;
    }
}
