using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Tests.Connections;

/// <summary> An operation the core handed to the test connection; the test plays the peer and the controller </summary>
internal sealed class PendingRequest(TestConnectionDriver connection, string kind, GattRequest? request)
{
    private readonly TestConnectionDriver _connection = connection;
    private readonly GattRequest? _request = request;
    private readonly TaskCompletionSource<object?> _done = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary> What the core asked for: "discovery", "read" or "command" </summary>
    public string Kind { get; } = kind;

    /// <summary> The request; a write without response has none </summary>
    public GattRequest Request =>
        _request ?? throw new InvalidOperationException("A write without response has no request");

    public Task<object?> Done => _done.Task;

    /// <summary> The controller sent the request's transaction </summary>
    public void Send() => Request.OnTransactionSent();

    /// <summary> Sends a discovery and answers it with these services, each with the connection's characteristic </summary>
    public void Complete(params BleUuid[] services)
    {
        Send();
        Answer(
            services
                .Select(uuid => (IRemoteServiceDriver)new TestRemoteService(uuid, _connection.Characteristic))
                .ToArray()
        );
    }

    /// <summary> The peer answered the transaction sent last </summary>
    public void Answer(object? result = null)
    {
        Request.OnTransactionAnswered();
        _done.TrySetResult(result);
    }

    /// <summary> The controller accepted a write without response </summary>
    public void Accept() => _done.TrySetResult(null);

    public void Fail(Exception exception) => _done.TrySetException(exception);
}
