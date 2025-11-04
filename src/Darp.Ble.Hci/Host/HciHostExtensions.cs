using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.Hci.Host;

/// <summary> Extensions which can be used to query commands, ... on the HCI Device </summary>
public static class HciHostExtensions
{
    /// <summary> Query a command expecting a <see cref="hciHost"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <typeparam name="TResponse"> The type of the parameters of the response packet </typeparam>
    /// <returns> The parameters of the response packet </returns>
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

    /// <summary> Query a command expecting a <see cref="HciCommandCompleteEvent{TParameters}"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="command"> The command to be sent </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <typeparam name="TParameters"> The type of the parameters of the response packet </typeparam>
    /// <returns> The parameters of the response packet </returns>
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

    /// <summary> Query a command expecting a <see cref="HciCommandStatusEvent"/> </summary>
    /// <param name="hciHost"> The hci host </param>
    /// <param name="command"> The command to be sent </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <typeparam name="TCommand"> The type of the command </typeparam>
    /// <returns> The command status </returns>
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

    public static Task<HciReadLocalSupportedCommandsResult> QueryReadLocalSupportedCommandsAsync(
        this HciHost host,
        CancellationToken token
    ) =>
        host.QueryCommandCompletionAsync<HciReadLocalSupportedCommandsCommand, HciReadLocalSupportedCommandsResult>(
            token
        );

    public static Task<HciResetResult> QueryResetAsync(this HciHost host, CancellationToken token) =>
        host.QueryCommandCompletionAsync<HciResetCommand, HciResetResult>(token);

    public static Task<HciLeReadLocalSupportedFeaturesResult> QueryLeReadLocalSupportedFeaturesAsync(
        this HciHost host,
        CancellationToken token
    ) =>
        host.QueryCommandCompletionAsync<HciLeReadLocalSupportedFeaturesCommand, HciLeReadLocalSupportedFeaturesResult>(
            token
        );

    public static Task<HciReadLocalVersionInformationResult> QueryReadLocalVersionInformationAsync(
        this HciHost host,
        CancellationToken token
    ) =>
        host.QueryCommandCompletionAsync<HciReadLocalVersionInformationCommand, HciReadLocalVersionInformationResult>(
            token
        );

    public static Task<HciReadLocalSupportedFeaturesResult> QueryReadLocalSupportedFeaturesAsync(
        this HciHost host,
        CancellationToken token
    ) =>
        host.QueryCommandCompletionAsync<HciReadLocalSupportedFeaturesCommand, HciReadLocalSupportedFeaturesResult>(
            token
        );

    public static Task<HciSetEventMaskResult> QuerySetEventMaskAsync(
        this HciHost host,
        HciSetEventMaskCommand command,
        CancellationToken token
    ) => host.QueryCommandCompletionAsync<HciSetEventMaskCommand, HciSetEventMaskResult>(command, token);

    public static Task<HciLeSetEventMaskResult> QueryLeSetEventMaskAsync(
        this HciHost host,
        HciLeSetEventMaskCommand command,
        CancellationToken token
    ) => host.QueryCommandCompletionAsync<HciLeSetEventMaskCommand, HciLeSetEventMaskResult>(command, token);

    public static Task<HciLeReadBufferSizeResultV1> QueryLeReadBufferSizeV1Async(
        this HciHost host,
        CancellationToken token
    ) => host.QueryCommandCompletionAsync<HciLeReadBufferSizeCommandV1, HciLeReadBufferSizeResultV1>(token);

    public static Task<HciLeReadSuggestedDefaultDataLengthResult> QueryLeReadSuggestedDefaultDataLengthAsync(
        this HciHost host,
        CancellationToken token
    ) =>
        host.QueryCommandCompletionAsync<
            HciLeReadSuggestedDefaultDataLengthCommand,
            HciLeReadSuggestedDefaultDataLengthResult
        >(token);

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
