using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Host;

/// <summary>
/// Provides convenience methods for commonly used HCI host commands.
/// </summary>
public static class HciHostExtensions
{
    /// <summary>
    /// Sends a parameterless HCI command and waits for its command-complete response.
    /// </summary>
    /// <param name="hciHost">The host used to send the command.</param>
    /// <param name="cancellationToken">Cancels the command while waiting for the response.</param>
    /// <typeparam name="TCommand">The HCI command type to send.</typeparam>
    /// <typeparam name="TResponse">The decoded command-complete response type.</typeparam>
    /// <returns>The decoded command-complete response.</returns>
    public static Task<TResponse> QueryCommandCompletionAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TCommand,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TResponse
    >(this HciHost hciHost, CancellationToken cancellationToken = default)
        where TCommand : unmanaged, IHciCommand
        where TResponse : unmanaged, ICommandStatusResult, IBinaryReadable<TResponse>
    {
        ArgumentNullException.ThrowIfNull(hciHost);
        return hciHost.QueryCommandCompletionAsync<TCommand, TResponse>(default, timeout: null, cancellationToken);
    }

    /// <summary>
    /// Sends an HCI command and waits for its command-complete response.
    /// </summary>
    /// <param name="hciHost">The host used to send the command.</param>
    /// <param name="command">The command to transmit.</param>
    /// <param name="cancellationToken">Cancels the command while waiting for the response.</param>
    /// <typeparam name="TCommand">The HCI command type to send.</typeparam>
    /// <typeparam name="TParameters">The decoded command-complete response type.</typeparam>
    /// <returns>The decoded command-complete response.</returns>
    public static Task<TParameters> QueryCommandCompletionAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TCommand,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TParameters
    >(this HciHost hciHost, TCommand command, CancellationToken cancellationToken = default)
        where TCommand : IHciCommand
        where TParameters : unmanaged, ICommandStatusResult, IBinaryReadable<TParameters>
    {
        ArgumentNullException.ThrowIfNull(hciHost);
        return hciHost.QueryCommandCompletionAsync<TCommand, TParameters>(command, timeout: null, cancellationToken);
    }

    /// <summary>
    /// Sends an HCI command and returns the command-status result.
    /// </summary>
    /// <param name="hciHost">The host used to send the command.</param>
    /// <param name="command">The command to transmit.</param>
    /// <param name="cancellationToken">Cancels the command while waiting for the response.</param>
    /// <typeparam name="TCommand">The HCI command type to send.</typeparam>
    /// <returns>The status reported by the controller.</returns>
    public static async Task<HciCommandStatus> QueryCommandStatusAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TCommand
    >(this HciHost hciHost, TCommand command = default, CancellationToken cancellationToken = default)
        where TCommand : unmanaged, IHciCommand
    {
        ArgumentNullException.ThrowIfNull(hciHost);
        HciCommandStatusEvent response = await hciHost
            .QueryCommandAsync<TCommand, HciCommandStatusEvent>(
                command,
                timeout: null,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        return response.Status;
    }
}
