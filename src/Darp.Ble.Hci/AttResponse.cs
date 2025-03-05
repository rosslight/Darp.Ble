using Darp.Ble.Hci.Payload.Att;

namespace Darp.Ble.Hci;

/// <summary> A response to an ATT request </summary>
/// <typeparam name="T"> The type of response att data </typeparam>
public readonly struct AttResponse<T> : IAttPdu
    where T : IAttPdu
{
    private AttResponse(bool isSuccess, T attValue, AttErrorRsp errorResponse)
    {
        IsSuccess = isSuccess;
        Value = attValue;
        Error = errorResponse;
    }

    /// <summary> The expected OpCode </summary>
    public static AttOpCode ExpectedOpCode => T.ExpectedOpCode;

    /// <summary> The value if successful </summary>
    public T Value { get; }

    /// <summary> The error response failed </summary>
    public AttErrorRsp Error { get; }

    /// <summary> True, if the response was not successful </summary>
    public bool IsError => !IsSuccess;

    /// <summary> True, if the response was successful </summary>
    public bool IsSuccess { get; }

    /// <summary> The OpCode of the response. Might be the OpCode of the error if IsError </summary>
    public AttOpCode OpCode => IsSuccess ? Value.OpCode : Error.OpCode;

    /// <summary> Create a new successful AttResponse </summary>
    /// <param name="attResponse"> The att response </param>
    /// <returns> A succeeded att response </returns>
    public static AttResponse<T> Ok(T attResponse) => new(isSuccess: true, attResponse, errorResponse: default);

    /// <summary> Create a failed AttResponse </summary>
    /// <param name="errorResponse"> The error response </param>
    /// <returns> A failed att respones </returns>
    public static AttResponse<T> Fail(AttErrorRsp errorResponse) => new(isSuccess: false, default!, errorResponse);
}
