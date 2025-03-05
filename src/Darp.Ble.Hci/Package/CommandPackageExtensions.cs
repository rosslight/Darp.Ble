using Darp.BinaryObjects;
using Darp.Ble.Hci.Exceptions;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;

namespace Darp.Ble.Hci.Package;

/// <summary> Extensions regarding command packets </summary>
public static class CommandPackageExtensions
{
    /// <summary> Query a command expecting a <see cref="HciCommandCompleteEvent{TParameters}"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="command"> The command to be sent </param>
    /// <param name="timeout"> The timeout waiting for the response </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <typeparam name="TResponse"> The type of the parameters of the response packet </typeparam>
    /// <returns> The parameters of the response packet </returns>
    public static async Task<TResponse> QueryCommandCompletionAsync<TCommand, TResponse>(
        this HciHost hciHost,
        TCommand command,
        TimeSpan? timeout,
        CancellationToken cancellationToken
    )
        where TCommand : IHciCommand
        where TResponse : IBinaryReadable<TResponse>
    {
        ArgumentNullException.ThrowIfNull(hciHost);
        HciCommandCompleteEvent response = await hciHost
            .QueryCommandAsync<TCommand, HciCommandCompleteEvent>(
                command,
                timeout: timeout,
                evt => evt.CommandOpCode == TCommand.OpCode,
                cancellationToken
            )
            .ConfigureAwait(false);
        if (!TResponse.TryReadLittleEndian(response.ReturnParameters.Span, out TResponse? parameters))
            throw new HciException("Command failed because response could not be read");
        return parameters;
    }

    /// <summary> Query a command expecting a <see cref="HciCommandCompleteEvent{TParameters}"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <typeparam name="TResponse"> The type of the parameters of the response packet </typeparam>
    /// <returns> The parameters of the response packet </returns>
    public static Task<TResponse> QueryCommandCompletionAsync<TCommand, TResponse>(
        this HciHost hciHost,
        CancellationToken cancellationToken = default
    )
        where TCommand : unmanaged, IHciCommand
        where TResponse : unmanaged, IBinaryReadable<TResponse>
    {
        return hciHost.QueryCommandCompletionAsync<TCommand, TResponse>(default, timeout: null, cancellationToken);
    }

    /// <summary> Query a command expecting a <see cref="HciCommandCompleteEvent{TParameters}"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="command"> The command to be sent </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <typeparam name="TParameters"> The type of the parameters of the response packet </typeparam>
    /// <returns> The parameters of the response packet </returns>
    public static Task<TParameters> QueryCommandCompletionAsync<TCommand, TParameters>(
        this HciHost hciHost,
        TCommand command,
        CancellationToken cancellationToken = default
    )
        where TCommand : IHciCommand
        where TParameters : unmanaged, IBinaryReadable<TParameters>
    {
        ArgumentNullException.ThrowIfNull(hciHost);
        return hciHost.QueryCommandCompletionAsync<TCommand, TParameters>(command, timeout: null, cancellationToken);
    }

    /// <summary> Query a command expecting a <see cref="HciCommandStatusEvent"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="command"> The command to be sent </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <returns> The command status </returns>
    public static Task<HciCommandStatus> QueryCommandStatusAsync<TCommand>(
        this HciHost hciHost,
        TCommand command = default,
        CancellationToken cancellationToken = default
    )
        where TCommand : unmanaged, IHciCommand
    {
        ArgumentNullException.ThrowIfNull(hciHost);
        return hciHost.QueryCommandStatusAsync(command, cancellationToken: cancellationToken);
    }
}
