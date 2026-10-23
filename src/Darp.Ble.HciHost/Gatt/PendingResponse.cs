using System.Runtime.CompilerServices;
using Darp.Ble.Backends;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload.Att;
using Darp.Utils.Messaging;

namespace Darp.Ble.HciHost.Gatt;

/// <summary> Waits for the answer of one ATT request: its response or an error response for its opcode </summary>
internal sealed partial class PendingResponse<TResponse>(AttOpCode requestOpCode, GattRequest gattRequest)
    : IPendingResponse
    where TResponse : IAttPdu
{
    private readonly AttOpCode _requestOpCode = requestOpCode;
    private readonly GattRequest _gattRequest = gattRequest;
    private readonly TaskCompletionSource<AttResponse<TResponse>> _answer = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    public Task<AttResponse<TResponse>> Task => _answer.Task;

    public void Fail(Exception exception) => _answer.TrySetException(exception);

    [MessageSink]
    private void OnPdu<T>(T pdu)
        where T : allows ref struct
    {
        AttResponse<TResponse> answer;
        if (typeof(T) == typeof(TResponse))
        {
            answer = AttResponse<TResponse>.Ok(Unsafe.As<T, TResponse>(ref pdu));
        }
        else if (typeof(T) == typeof(AttErrorRsp))
        {
            AttErrorRsp error = Unsafe.As<T, AttErrorRsp>(ref pdu);
            if (error.RequestOpCode != _requestOpCode)
                return;
            answer = AttResponse<TResponse>.Fail(error);
        }
        else
        {
            return;
        }
        if (_answer.Task.IsCompleted)
            return;
        _gattRequest.OnTransactionAnswered();
        _answer.TrySetResult(answer);
    }
}
