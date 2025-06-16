using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Services;

#pragma warning disable MA0048 // File name must match type name

/// <summary> The service contract for an echo service </summary>
public static class EchoServiceContract
{
    /// <summary> Represents a simple request </summary>
    public delegate ValueTask<byte[]> SimpleRequestHandler(byte[] byteArray);

    /// <summary> Represents a request handler </summary>
    private delegate ValueTask<byte[]> RequestHandler(byte[] byteArray);

    /// <summary> Add an echo service to the peripheral </summary>
    /// <param name="peripheral"> The peripheral to add the service to </param>
    /// <param name="serviceUuid"> The uuid of the service </param>
    /// <param name="writeUuid"> The uuid of the write characteristic </param>
    /// <param name="notifyUuid"> The uuid of the notify characteristic </param>
    /// <param name="handleRequest"> A function which defines how a request is handled. Defaults to just returning the received bytes </param>
    /// <returns> A wrapper with the discovered characteristics </returns>
    private static GattClientEchoService AddEchoService(
        this IBlePeripheral peripheral,
        BleUuid serviceUuid,
        BleUuid writeUuid,
        BleUuid notifyUuid,
        RequestHandler? handleRequest = null
    )
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        handleRequest ??= ValueTask.FromResult;

        // Add the client service
        IGattClientService service = peripheral.AddService(serviceUuid, isPrimary: true);

        // Add the mandatory write characteristic
        GattClientCharacteristic<Properties.Notify> notifyCharacteristic = null!;
        GattClientCharacteristic<Properties.Write> writeCharacteristic = service.AddCharacteristic<Properties.Write>(
            writeUuid,
            onWrite: (peer, bytes) =>
            {
                ValueTask<byte[]> valueTask = handleRequest(bytes);
                // ReSharper disable once AccessToModifiedClosure
                // We expect onWrite to not execute before the notify characteristic was added
                RespondToRequest(peer, notifyCharacteristic, valueTask);
                return ValueTask.FromResult(GattProtocolStatus.Success);
            }
        );

        // Add the mandatory notify characteristic
        notifyCharacteristic = service.AddCharacteristic<Properties.Notify>(notifyUuid);

        return new GattClientEchoService(service) { Write = writeCharacteristic, Notify = notifyCharacteristic };
    }

    /// <summary> Add an echo service to the peripheral </summary>
    /// <param name="peripheral"> The peripheral to add the service to </param>
    /// <param name="serviceUuid"> The uuid of the service </param>
    /// <param name="writeUuid"> The uuid of the write characteristic </param>
    /// <param name="notifyUuid"> The uuid of the notify characteristic </param>
    /// <param name="handleRequest"> A function which defines how a request is handled. Defaults to just returning the received bytes </param>
    /// <returns> A wrapper with the discovered characteristics </returns>
    public static GattClientEchoService AddEchoService(
        this IBlePeripheral peripheral,
        BleUuid serviceUuid,
        BleUuid writeUuid,
        BleUuid notifyUuid,
        SimpleRequestHandler? handleRequest = null
    )
    {
        return peripheral.AddEchoService(
            serviceUuid,
            writeUuid,
            notifyUuid,
            handleRequest is null ? null : (RequestHandler)(bytes => handleRequest(bytes))
        );
    }

    private static async void RespondToRequest(
        IGattClientPeer? peer,
        GattClientCharacteristic<Properties.Notify> notifyCharacteristic,
        ValueTask<byte[]> valueTask
    )
    {
        try
        {
            byte[] responseBytes = await valueTask.ConfigureAwait(false);
            await notifyCharacteristic.NotifyAsync(peer, responseBytes).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            var provider = ((IGattClientCharacteristic<Properties.Notify>)notifyCharacteristic).ServiceProvider;
            var logger = provider.GetRequiredService<ILogger<GattClientEchoService>>();
            logger.LogError(e, "Handler has thrown");
        }
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
            .DiscoverCharacteristicAsync<Properties.Write>(writeUuid, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // Discover the notify characteristic
        IGattServerCharacteristic<Properties.Notify> char2 = await service
            .DiscoverCharacteristicAsync<Properties.Notify>(notifyUuid, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return new GattServerEchoService(service) { Write = char1, Notify = char2 };
    }
}

/// <summary> The EchoService wrapper representing the gatt client </summary>
public sealed class GattClientEchoService(IGattClientService service) : GattClientServiceProxy(service)
{
    /// <summary> The write characteristic </summary>
    public required IGattClientCharacteristic<Properties.Write> Write { get; init; }

    /// <summary> The notify characteristic </summary>
    public required IGattClientCharacteristic<Properties.Notify> Notify { get; init; }
}

/// <summary> The EchoService wrapper representing the gatt server </summary>
public sealed class GattServerEchoService(IGattServerService service) : GattServerServiceProxy(service)
{
    /// <summary> The write characteristic </summary>
    public required IGattServerCharacteristic<Properties.Write> Write { get; init; }

    /// <summary> The notify characteristic </summary>
    public required IGattServerCharacteristic<Properties.Notify> Notify { get; init; }

    /// <summary> Subscribe to all notifications </summary>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A disposable to unsubscribe from notifications </returns>
    public async Task<IAsyncDisposable> EnableNotificationsAsync(CancellationToken cancellationToken = default)
    {
        // Do not do anything but subscribe
        return await Notify.OnNotifyAsync(_ => { }, cancellationToken: cancellationToken).ConfigureAwait(false);
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
            Task<byte[]> notifyConnected = disposableObs.Timeout(timeout.Value).FirstAsync().ToTask(cancellationToken);
            await Write.WriteAsync(requestBytes, cancellationToken).ConfigureAwait(false);
            return await notifyConnected.ConfigureAwait(false);
        }
    }
}
