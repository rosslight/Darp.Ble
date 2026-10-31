using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Services;

/// <summary> An echo service: a central writes a request, the peripheral notifies the reply to that central </summary>
public static class EchoServiceContract
{
    /// <summary> Adds an echo service </summary>
    /// <param name="peripheral"> The adapter's peripheral role </param>
    /// <param name="serviceUuid"> The UUID of the service </param>
    /// <param name="writeUuid"> The UUID of the characteristic requests are written to </param>
    /// <param name="notifyUuid"> The UUID of the characteristic replies are notified on </param>
    /// <param name="handleRequest"> Answers a request; replies with the request itself when null </param>
    /// <param name="cancellationToken"> Cancels adding </param>
    /// <returns> The service </returns>
    public static async Task<LocalEchoService> AddEchoServiceAsync(
        this BlePeripheral peripheral,
        BleUuid serviceUuid,
        BleUuid writeUuid,
        BleUuid notifyUuid,
        EchoRequestHandler? handleRequest = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        EchoRequestHandler handle = handleRequest ?? ((_, request, _) => ValueTask.FromResult(request.ToArray()));
        var builder = new LocalGattServiceBuilder(serviceUuid);
        // Added first, so the write handler can reply on it
        LocalCharacteristic<Properties.Notify> notify = builder.AddCharacteristic<Properties.Notify>(notifyUuid);
        LocalCharacteristic<Properties.Write> write = builder.AddCharacteristic<Properties.Write>(
            writeUuid,
            onWrite: async (central, request, ct) =>
            {
                byte[] reply = await handle(central, request, ct).ConfigureAwait(false);
                await notify.NotifyAsync(central, reply, ct).ConfigureAwait(false);
                return GattProtocolStatus.Success;
            }
        );
        LocalGattService service = await peripheral.AddServiceAsync(builder, cancellationToken).ConfigureAwait(false);
        return new LocalEchoService(service, write, notify);
    }

    /// <summary> Discovers an echo service of a connected peripheral </summary>
    /// <param name="connection"> The connection to the peripheral </param>
    /// <param name="serviceUuid"> The UUID of the service </param>
    /// <param name="writeUuid"> The UUID of the characteristic requests are written to </param>
    /// <param name="notifyUuid"> The UUID of the characteristic replies are notified on </param>
    /// <param name="cancellationToken"> Cancels discovery </param>
    /// <returns> The service; <see cref="RemoteEchoService.QueryOneAsync"/> sends a request and waits for its reply </returns>
    public static async Task<RemoteEchoService> DiscoverEchoServiceAsync(
        this ConnectedPeripheral connection,
        BleUuid serviceUuid,
        BleUuid writeUuid,
        BleUuid notifyUuid,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        RemoteGattService service = await connection
            .DiscoverServiceAsync(serviceUuid, cancellationToken)
            .ConfigureAwait(false);
        RemoteCharacteristic<Properties.Write> write = await service
            .DiscoverCharacteristicAsync(new CharacteristicDeclaration<Properties.Write>(writeUuid), cancellationToken)
            .ConfigureAwait(false);
        RemoteCharacteristic<Properties.Notify> notify = await service
            .DiscoverCharacteristicAsync(
                new CharacteristicDeclaration<Properties.Notify>(notifyUuid),
                cancellationToken
            )
            .ConfigureAwait(false);
        return new RemoteEchoService(write, notify, service.Connection.TimeProvider);
    }
}
