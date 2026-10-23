using System.Threading.Channels;
using Darp.Ble.Backends;

namespace Darp.Ble.Tests.Connections;

/// <summary> A connection whose operations wait until the test answers them; the MTU exchange answers at once </summary>
internal sealed class TestConnectionDriver : IConnectionDriver
{
    private readonly Channel<PendingRequest> _handedOver = Channel.CreateUnbounded<PendingRequest>();
    private readonly List<PendingRequest> _running = [];

    public TestConnectionDriver(IConnectionSink sink)
    {
        Sink = sink;
        Characteristic = new TestRemoteCharacteristic(this);
    }

    /// <summary> Reports the connection's events to the core </summary>
    public IConnectionSink Sink { get; }

    /// <summary> The characteristic every discovered service has </summary>
    public TestRemoteCharacteristic Characteristic { get; }

    /// <summary> How many operations the core handed over </summary>
    public int HandedOverCount { get; private set; }

    public int DisposeCount { get; private set; }

    /// <summary> Waits for the next operation the core hands over </summary>
    public ValueTask<PendingRequest> NextRequestAsync(CancellationToken cancellationToken) =>
        _handedOver.Reader.ReadAsync(cancellationToken);

    public Task<int> ExchangeMtuAsync(int clientMtu, GattRequest request) => Task.FromResult(Math.Min(clientMtu, 247));

    public async Task<IReadOnlyList<IRemoteServiceDriver>> DiscoverServicesAsync(GattRequest request) =>
        (IReadOnlyList<IRemoteServiceDriver>?)await HandOver("discovery", request).Done.ConfigureAwait(false) ?? [];

    /// <summary> Records an operation the core handed over </summary>
    public PendingRequest HandOver(string kind, GattRequest request)
    {
        var pending = new PendingRequest(this, kind, request);
        lock (_running)
        {
            HandedOverCount++;
            _running.Add(pending);
        }
        _handedOver.Writer.TryWrite(pending);
        return pending;
    }

    /// <summary> Ends every operation still running, as a driver does when its link goes </summary>
    public ValueTask DisposeAsync()
    {
        PendingRequest[] running;
        lock (_running)
        {
            DisposeCount++;
            running = [.. _running];
        }
        foreach (PendingRequest pending in running)
            pending.Fail(new IOException("The link is down"));
        return ValueTask.CompletedTask;
    }
}
