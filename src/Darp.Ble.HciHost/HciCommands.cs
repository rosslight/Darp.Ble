using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.HciHost;

internal static class HciCommands
{
    /// <summary> Runs a command and fails with a <see cref="BleException"/> carrying the controller's status unless it succeeded </summary>
    public static async Task<TResult> RunAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TCommand,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TResult
    >(this HciDevice device, TCommand command, string action, CancellationToken cancellationToken)
        where TCommand : IHciCommand
        where TResult : unmanaged, ICommandStatusResult, IBinaryReadable<TResult>
    {
        TResult result = await device
            .Host.QueryCommandCompletionAsync<TCommand, TResult>(command, cancellationToken)
            .ConfigureAwait(false);
        if (result.Status is not HciCommandStatus.Success)
            throw Failed(result.Status, action);
        return result;
    }

    public static BleException Failed(HciCommandStatus status, string action) =>
        new($"The controller could not {action}: {status}", false, (int)status, null);
}
