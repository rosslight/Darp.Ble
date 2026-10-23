using System.Threading.Channels;
using Darp.Ble.Backends;

namespace Darp.Ble.Tests.Connections;

/// <summary> A connection whose service discoveries wait until the test answers them; the MTU exchange answers at once </summary>
internal sealed class TestConnectionDriver(IConnectionSink sink) : IConnectionDriver
{
    private readonly Channel<PendingRequest> _handedOver = Channel.CreateUnbounded<PendingRequest>();
    private readonly List<PendingRequest> _running = [];

    /// <summary> Reports the connection's events to the core </summary>
    public IConnectionSink Sink { get; } = sink;

    /// <summary> How many discoveries the core handed over </summary>
    public int HandedOverCount { get; private set; }

    public int DisposeCount { get; private set; }

    /// <summary> Waits for the next discovery the core hands over </summary>
    public ValueTask<PendingRequest> NextRequestAsync(CancellationToken cancellationToken) =>
        _handedOver.Reader.ReadAsync(cancellationToken);

    public Task<int> ExchangeMtuAsync(int clientMtu, GattRequest request) => Task.FromResult(Math.Min(clientMtu, 247));

    public Task<IReadOnlyList<IRemoteServiceDriver>> DiscoverServicesAsync(GattRequest request)
    {
        var pending = new PendingRequest(request);
        lock (_running)
        {
            HandedOverCount++;
            _running.Add(pending);
        }
        _handedOver.Writer.TryWrite(pending);
        return pending.Answer;
    }

    /// <summary> Ends every discovery still running, as a driver does when its link goes </summary>
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
