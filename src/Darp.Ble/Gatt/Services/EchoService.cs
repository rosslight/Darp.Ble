using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;

namespace Darp.Ble.Gatt.Services;

#pragma warning disable MA0048 // File name must match type name

/// <summary> The service contract for an echo service </summary>
public static class EchoServiceContract
{
    /// <summary> Add an echo service to the peripheral </summary>
    /// <param name="peripheral"> The peripheral to add the service to </param>
    /// <param name="serviceUuid"> The uuid of the service </param>
    /// <param name="writeUuid"> The uuid of the write characteristic </param>
    /// <param name="notifyUuid"> The uuid of the notify characteristic </param>
    /// <param name="handleRequest"> A function which defines how a request is handled. Defaults to <see cref="Observable.Return{TResult}(TResult)"/> </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> A wrapper with the discovered characteristics </returns>
    public static async Task<GattClientEchoService> AddEchoServiceAsync(
        this IBlePeripheral peripheral,
        BleUuid serviceUuid,
        BleUuid writeUuid,
        BleUuid notifyUuid,
        Func<byte[], IObservable<byte[]>>? handleRequest = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        handleRequest ??= Observable.Return;

        // Add the client service
        IGattClientService service = await peripheral
            .AddServiceAsync(serviceUuid, isPrimary: true, cancellationToken)
            .ConfigureAwait(false);

        // Add the mandatory write characteristic
        IGattClientCharacteristic<Properties.Notify> notifyCharacteristic = null!;
        IGattClientCharacteristic<Properties.Write> writeCharacteristic = await service
            .AddCharacteristicAsync<Properties.Write>(
                writeUuid,
                onWrite: (peer, bytes) =>
                {
                    IObservable<byte[]> responseObservable = handleRequest(bytes);
                    // ReSharper disable once AccessToModifiedClosure
                    // We expect onWrite to not execute before the notify characteristic was added
                    _ = responseObservable.Subscribe(responseBytes =>
                        notifyCharacteristic.Notify(peer, responseBytes)
                    );
                    return GattProtocolStatus.Success;
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        // Add the mandatory notify characteristic
        notifyCharacteristic = await service
            .AddCharacteristicAsync<Properties.Notify>(
                notifyUuid,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return new GattClientEchoService(service)
        {
            Write = writeCharacteristic,
            Notify = notifyCharacteristic,
        };
    }

    /// <summary> Discover the echo server </summary>
    /// <param name="serverPeer"> The peer to discover the service from </param>
    /// <param name="serviceUuid"> The uuid of the service </param>
    /// <param name="writeUuid"> The uuid of the write characteristic </param>
    /// <param name="notifyUuid"> The uuid of the notify characteristic </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> A wrapper with the discovered characteristics </returns>
    public static async Task<GattServerEchoService> DiscoverEchoServiceAsync(
        this IGattServerPeer serverPeer,
        BleUuid serviceUuid,
        BleUuid writeUuid,
        BleUuid notifyUuid,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(serverPeer);

        // Discover the service
        IGattServerService service = await serverPeer
            .DiscoverServiceAsync(serviceUuid, cancellationToken)
            .ConfigureAwait(false);

        // Discover the write characteristic
        IGattServerCharacteristic<Properties.Write> char1 = await service
            .DiscoverCharacteristicAsync<Properties.Write>(
                writeUuid,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        // Discover the notify characteristic
        IGattServerCharacteristic<Properties.Notify> char2 = await service
            .DiscoverCharacteristicAsync<Properties.Notify>(
                notifyUuid,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        return new GattServerEchoService(service) { Write = char1, Notify = char2 };
    }
}

/// <summary> The EchoService wrapper representing the gatt client </summary>
public sealed class GattClientEchoService(IGattClientService service)
    : GattClientServiceProxy(service)
{
    /// <summary> The write characteristic </summary>
    public required IGattClientCharacteristic<Properties.Write> Write { get; init; }

    /// <summary> The notify characteristic </summary>
    public required IGattClientCharacteristic<Properties.Notify> Notify { get; init; }
}

/// <summary> The EchoService wrapper representing the gatt server </summary>
public sealed class GattServerEchoService(IGattServerService service)
    : GattServerServiceProxy(service)
{
    /// <summary> The write characteristic </summary>
    public required IGattServerCharacteristic<Properties.Write> Write { get; init; }

    /// <summary> The notify characteristic </summary>
    public required IGattServerCharacteristic<Properties.Notify> Notify { get; init; }

    /// <summary> Subscribe to all notifications </summary>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A disposable to unsubscribe from notifications </returns>
    public async Task<IAsyncDisposable> EnableNotificationsAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await Notify
            .OnNotifyAsync(
                _ =>
                {
                    // Do not do anything but subscribe
                },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <summary> Query one request from the echo service </summary>
    /// <param name="requestBytes"> The bytes of the request </param>
    /// <param name="timeout"> The timeout. Default is 10 seconds </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> The bytes returned by the echo service </returns>
    public async Task<byte[]> QueryOneAsync(
        byte[] requestBytes,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
    {
        timeout ??= TimeSpan.FromSeconds(10);
        IDisposableObservable<byte[]> disposableObs = await Notify
            .OnNotifyAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (disposableObs.ConfigureAwait(false))
        {
            Task<byte[]> notifyConnected = disposableObs
                .Timeout(timeout.Value)
                .FirstAsync()
                .ToTask(cancellationToken);
            await Write.WriteAsync(requestBytes, cancellationToken).ConfigureAwait(false);
            return await notifyConnected.ConfigureAwait(false);
        }
    }
}
