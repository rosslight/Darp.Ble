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

    /// <summary>Reads the controller's supported HCI commands.</summary>
    /// <param name="host">The host used to send the command.</param>
    /// <param name="token">Cancels the command while waiting for the response.</param>
    /// <returns>The supported command bitfield reported by the controller.</returns>
    public static Task<HciReadLocalSupportedCommandsResult> QueryReadLocalSupportedCommandsAsync(
        this HciHost host,
        CancellationToken token
    ) =>
        host.QueryCommandCompletionAsync<HciReadLocalSupportedCommandsCommand, HciReadLocalSupportedCommandsResult>(
            token
        );

    /// <summary>Resets the controller.</summary>
    /// <param name="host">The host used to send the command.</param>
    /// <param name="token">Cancels the command while waiting for the response.</param>
    /// <returns>The reset command result.</returns>
    public static Task<HciResetResult> QueryResetAsync(this HciHost host, CancellationToken token) =>
        host.QueryCommandCompletionAsync<HciResetCommand, HciResetResult>(token);

    /// <summary>Reads the controller's LE supported feature set.</summary>
    /// <param name="host">The host used to send the command.</param>
    /// <param name="token">Cancels the command while waiting for the response.</param>
    /// <returns>The LE supported features reported by the controller.</returns>
    public static Task<HciLeReadLocalSupportedFeaturesResult> QueryLeReadLocalSupportedFeaturesAsync(
        this HciHost host,
        CancellationToken token
    ) =>
        host.QueryCommandCompletionAsync<HciLeReadLocalSupportedFeaturesCommand, HciLeReadLocalSupportedFeaturesResult>(
            token
        );

    /// <summary>Reads the controller's local version information.</summary>
    /// <param name="host">The host used to send the command.</param>
    /// <param name="token">Cancels the command while waiting for the response.</param>
    /// <returns>The local version information reported by the controller.</returns>
    public static Task<HciReadLocalVersionInformationResult> QueryReadLocalVersionInformationAsync(
        this HciHost host,
        CancellationToken token
    ) =>
        host.QueryCommandCompletionAsync<HciReadLocalVersionInformationCommand, HciReadLocalVersionInformationResult>(
            token
        );

    /// <summary>Reads the controller's BR/EDR supported feature set.</summary>
    /// <param name="host">The host used to send the command.</param>
    /// <param name="token">Cancels the command while waiting for the response.</param>
    /// <returns>The supported features reported by the controller.</returns>
    public static Task<HciReadLocalSupportedFeaturesResult> QueryReadLocalSupportedFeaturesAsync(
        this HciHost host,
        CancellationToken token
    ) =>
        host.QueryCommandCompletionAsync<HciReadLocalSupportedFeaturesCommand, HciReadLocalSupportedFeaturesResult>(
            token
        );

    /// <summary>Sets the controller event mask.</summary>
    /// <param name="host">The host used to send the command.</param>
    /// <param name="command">The event mask command to apply.</param>
    /// <param name="token">Cancels the command while waiting for the response.</param>
    /// <returns>The event-mask command result.</returns>
    public static Task<HciSetEventMaskResult> QuerySetEventMaskAsync(
        this HciHost host,
        HciSetEventMaskCommand command,
        CancellationToken token
    ) => host.QueryCommandCompletionAsync<HciSetEventMaskCommand, HciSetEventMaskResult>(command, token);

    /// <summary>Sets the controller LE event mask.</summary>
    /// <param name="host">The host used to send the command.</param>
    /// <param name="command">The LE event mask command to apply.</param>
    /// <param name="token">Cancels the command while waiting for the response.</param>
    /// <returns>The LE event-mask command result.</returns>
    public static Task<HciLeSetEventMaskResult> QueryLeSetEventMaskAsync(
        this HciHost host,
        HciLeSetEventMaskCommand command,
        CancellationToken token
    ) => host.QueryCommandCompletionAsync<HciLeSetEventMaskCommand, HciLeSetEventMaskResult>(command, token);

    /// <summary>Reads the controller's LE ACL buffer sizes.</summary>
    /// <param name="host">The host used to send the command.</param>
    /// <param name="token">Cancels the command while waiting for the response.</param>
    /// <returns>The LE buffer size information reported by the controller.</returns>
    public static Task<HciLeReadBufferSizeResultV1> QueryLeReadBufferSizeV1Async(
        this HciHost host,
        CancellationToken token
    ) => host.QueryCommandCompletionAsync<HciLeReadBufferSizeCommandV1, HciLeReadBufferSizeResultV1>(token);

    /// <summary>Reads the controller's suggested default LE data length.</summary>
    /// <param name="host">The host used to send the command.</param>
    /// <param name="token">Cancels the command while waiting for the response.</param>
    /// <returns>The suggested default LE data length.</returns>
    public static Task<HciLeReadSuggestedDefaultDataLengthResult> QueryLeReadSuggestedDefaultDataLengthAsync(
        this HciHost host,
        CancellationToken token
    ) =>
        host.QueryCommandCompletionAsync<
            HciLeReadSuggestedDefaultDataLengthCommand,
            HciLeReadSuggestedDefaultDataLengthResult
        >(token);

    /// <summary>Writes the controller's suggested default LE data length.</summary>
    /// <param name="host">The host used to send the command.</param>
    /// <param name="command">The suggested default data length to apply.</param>
    /// <param name="token">Cancels the command while waiting for the response.</param>
    /// <returns>The result of the write command.</returns>
    public static Task<HciLeWriteSuggestedDefaultDataLengthResult> QueryLeWriteSuggestedDefaultDataLengthAsync(
        this HciHost host,
        HciLeWriteSuggestedDefaultDataLengthCommand command,
        CancellationToken token
    ) =>
        host.QueryCommandCompletionAsync<
            HciLeWriteSuggestedDefaultDataLengthCommand,
            HciLeWriteSuggestedDefaultDataLengthResult
        >(command, token);
}
