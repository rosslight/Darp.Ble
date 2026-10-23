using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Tests.Connections;

/// <summary> A discovery the core handed to the test connection; the test plays the peer </summary>
internal sealed class PendingRequest(GattRequest request)
{
    private readonly TaskCompletionSource<IReadOnlyList<IRemoteServiceDriver>> _answer = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    public GattRequest Request { get; } = request;

    public Task<IReadOnlyList<IRemoteServiceDriver>> Answer => _answer.Task;

    /// <summary> Sends the request and answers it with these services </summary>
    public void Complete(params BleUuid[] services)
    {
        Request.OnTransactionSent();
        Request.OnTransactionAnswered();
        _answer.TrySetResult([.. services.Select(uuid => new TestRemoteService(uuid))]);
    }

    public void Fail(Exception exception) => _answer.TrySetException(exception);
}
