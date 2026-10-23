using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Tests.Connections;

/// <summary> Connects at once to every address; the test drives each connection through <see cref="Connection"/> </summary>
internal sealed class TestCentralDriver : ICentralDriver
{
    private readonly TaskCompletionSource<TestConnectionDriver> _connected = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    /// <summary> The connection opened last </summary>
    public Task<TestConnectionDriver> Connection => _connected.Task;

    public Task<IConnectionDriver> ConnectAsync(
        BleAddress address,
        BleConnectionParameters parameters,
        IConnectionSink sink,
        CancellationToken cancellationToken
    )
    {
        var connection = new TestConnectionDriver(sink);
        _connected.TrySetResult(connection);
        return Task.FromResult<IConnectionDriver>(connection);
    }
}
