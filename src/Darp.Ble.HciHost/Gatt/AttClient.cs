using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Darp.BinaryObjects;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload.Att;
using Darp.Utils.Messaging;

namespace Darp.Ble.HciHost.Gatt;

/// <summary>
/// The ATT client of one link. The core sends one request at a time (D11) and owns the ATT timeout,
/// so a request waits for its answer without a timeout of its own until the link ends
/// </summary>
internal sealed class AttClient(AclConnection connection)
{
    private readonly AclConnection _connection = connection;
    private readonly Lock _lock = new();
    private IPendingResponse? _pending;
    private Exception? _ended;

    /// <summary> Sends one request as one transaction and waits for its answer </summary>
    /// <exception cref="GattException"> The peer answered with an ATT error </exception>
    public async Task<TResponse> QueryAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TRequest,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TResponse
    >(TRequest request, GattRequest gattRequest)
        where TRequest : IAttPdu, IBinaryWritable
        where TResponse : struct, IAttPdu, IBinaryReadable<TResponse>
    {
        AttResponse<TResponse> response = await SendAsync<TRequest, TResponse>(request, gattRequest)
            .ConfigureAwait(false);
        return response.IsError ? throw Failed(request.OpCode, response.Error) : response.Value;
    }

    /// <summary> Like <see cref="QueryAsync{TRequest,TResponse}"/>, but null when the peer has no attribute in the range, which ends a discovery </summary>
    public async Task<TResponse?> QueryUnlessNotFoundAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TRequest,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TResponse
    >(TRequest request, GattRequest gattRequest)
        where TRequest : IAttPdu, IBinaryWritable
        where TResponse : struct, IAttPdu, IBinaryReadable<TResponse>
    {
        AttResponse<TResponse> response = await SendAsync<TRequest, TResponse>(request, gattRequest)
            .ConfigureAwait(false);
        if (!response.IsError)
            return response.Value;
        return response.Error.ErrorCode is AttErrorCode.AttributeNotFoundError
            ? null
            : throw Failed(request.OpCode, response.Error);
    }

    private static GattException Failed(AttOpCode requestOpCode, AttErrorRsp error) =>
        new((GattProtocolStatus)error.ErrorCode, $"The peer answered {requestOpCode} with {error.ErrorCode}");

    private async Task<AttResponse<TResponse>> SendAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TRequest,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TResponse
    >(TRequest request, GattRequest gattRequest)
        where TRequest : IAttPdu, IBinaryWritable
        where TResponse : struct, IAttPdu, IBinaryReadable<TResponse>
    {
        gattRequest.HandOffToken.ThrowIfCancellationRequested();
        var pending = new PendingResponse<TResponse>(request.OpCode, gattRequest);
        lock (_lock)
        {
            if (_ended is not null)
                throw new BleException("The link is down", _ended);
            _pending = pending;
        }
        // The answer is awaited before the request leaves, so an early answer cannot be missed
        using IDisposable subscription = _connection.Assembler.Subscribe(pending);
        gattRequest.OnTransactionSent();
        _connection.EnqueueGattPacket(request, activity: null);
        try
        {
            return await pending.Task.ConfigureAwait(false);
        }
        finally
        {
            lock (_lock)
            {
                if (ReferenceEquals(_pending, pending))
                    _pending = null;
            }
        }
    }

    /// <summary> The link ended: the waiting request fails and no new one starts </summary>
    public void End(Exception reason)
    {
        IPendingResponse? pending;
        lock (_lock)
        {
            _ended ??= reason;
            pending = _pending;
            _pending = null;
        }
        pending?.Fail(new BleException("The link went down while waiting for an answer", reason));
    }
}
