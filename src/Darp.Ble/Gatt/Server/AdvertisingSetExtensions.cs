using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;

namespace Darp.Ble.Gatt.Server;

/// <summary> Extensions of <see cref="IAdvertisingSet"/> </summary>
public static class AdvertisingSetExtensions
{
    /// <summary> Create an advertising set using a legacy pdu </summary>
    /// <param name="broadcaster"> The broadcaster to advertise the advertisements </param>
    /// <param name="type"> The type of the advertisements to be advertised </param>
    /// <param name="peerAddress"> An optional address to whom the advertisements will be directed to </param>
    /// <param name="data"> The data to be advertised </param>
    /// <param name="scanResponseData"> The data to return on scan responses </param>
    /// <param name="interval"> The interval to advertise in </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> An async disposable to stop the broadcast </returns>
    public static async Task<IAdvertisingSet> CreateAdvertisingSetAsync(
        this IBleBroadcaster broadcaster,
        BleEventType type = BleEventType.AdvInd,
        BleAddress? peerAddress = null,
        AdvertisingData? data = null,
        AdvertisingData? scanResponseData = null,
        ScanTiming interval = ScanTiming.Ms1000,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(broadcaster);
        var parameters = new AdvertisingParameters
        {
            Type = type,
            PeerAddress = peerAddress,
            MinPrimaryAdvertisingInterval = interval,
            MaxPrimaryAdvertisingInterval = interval,
        };
        return await broadcaster
            .CreateAdvertisingSetAsync(parameters, data, scanResponseData, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary> Start advertising using a legacy pdu. Under the hood, advertising sets are used </summary>
    /// <param name="broadcaster"> The broadcaster to advertise the advertisements </param>
    /// <param name="type"> The type of the advertisements to be advertised </param>
    /// <param name="peerAddress"> An optional address to whom the advertisements will be directed to </param>
    /// <param name="data"> The data to be advertised </param>
    /// <param name="scanResponseData"> The data to return on scan responses </param>
    /// <param name="interval"> The interval to advertise in </param>
    /// <param name="autoRestart"> If true, advertising will be restarted after a peripheral disconnected </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> An async disposable to stop the broadcast </returns>
    public static async Task<IAsyncDisposable> StartAdvertisingAsync(
        this IBleBroadcaster broadcaster,
        BleEventType type = BleEventType.AdvInd,
        BleAddress? peerAddress = null,
        AdvertisingData? data = null,
        AdvertisingData? scanResponseData = null,
        ScanTiming interval = ScanTiming.Ms1000,
        bool autoRestart = false,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(broadcaster);
        IAdvertisingSet set = await broadcaster
            .CreateAdvertisingSetAsync(type, peerAddress, data, scanResponseData, interval, cancellationToken)
            .ConfigureAwait(false);
        if (autoRestart)
            return await set.StartAdvertisingAndRestartAsync(cancellationToken).ConfigureAwait(false);
        return await set.StartAdvertisingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary> Start advertising a specific advertising set </summary>
    /// <param name="broadcaster"> The broadcaster to advertise the sets for </param>
    /// <param name="set"> The advertising set to broadcast </param>
    /// <param name="sets"> Additional advertising sets </param>
    /// <returns></returns>
    public static Task<IAsyncDisposable> StartAdvertisingAsync(
        this IBleBroadcaster broadcaster,
        IAdvertisingSet set,
        params IEnumerable<IAdvertisingSet> sets
    )
    {
        ArgumentNullException.ThrowIfNull(broadcaster);

        (IAdvertisingSet, TimeSpan, byte)[] advertisingSetStartInfo = sets.Prepend(set)
            .Select(x => (x, TimeSpan.Zero, (byte)0))
            .ToArray();
        return broadcaster.StartAdvertisingAsync(advertisingSetStartInfo, CancellationToken.None);
    }

    /// <summary> Start broadcasting an advertising set using its broadcaster </summary>
    /// <param name="set"> The advertising set to broadcast </param>
    /// <param name="duration"> The duration to broadcast for. TimeSpan.<see cref="TimeSpan.Zero"/> skips this constraint </param>
    /// <param name="numberOfEvents"> The number of events to advertise for. <c>0</c> skips this constraint </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> An async disposable which allows cancellation of the broadcast </returns>
    public static Task<IAsyncDisposable> StartAdvertisingAsync(
        this IAdvertisingSet set,
        TimeSpan duration = default,
        byte numberOfEvents = 0,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(set);
        return set.Broadcaster.StartAdvertisingAsync([(set, duration, numberOfEvents)], cancellationToken);
    }

    /// <summary> Start advertising and automatically restart the advertisement upon disconnection events. </summary>
    /// <param name="set">The advertising set used to manage the advertising operation.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An async disposable object to stop the advertisement and disconnect handlers.</returns>
    /// <exception cref="NotSupportedException">Thrown when the device does not support the peripheral role.</exception>
    public static async Task<IAsyncDisposable> StartAdvertisingAndRestartAsync(
        this IAdvertisingSet set,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(set);
        if (!set.Broadcaster.Device.Capabilities.HasFlag(Capabilities.Peripheral))
            throw new NotSupportedException("Device does not support peripheral role. Listen to disconnection events");
        IAsyncDisposable advertisingDisposable;
        IDisposable autoRestartDisposable = set.Broadcaster.Device.Peripheral.WhenDisconnected.Subscribe(__ =>
        {
            _ = Task.Run(
                async () =>
                {
                    advertisingDisposable = await set.StartAdvertisingAsync(cancellationToken: CancellationToken.None)
                        .ConfigureAwait(false);
                },
                CancellationToken.None
            );
        });

        advertisingDisposable = await set.StartAdvertisingAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return AsyncDisposable.Create(async () =>
        {
            autoRestartDisposable.Dispose();
            await advertisingDisposable.DisposeAsync().ConfigureAwait(false);
        });
    }

    /// <summary> Set the advertising parameters. If the set was already advertising, stops the advertising train, sets parameters and restarts it. </summary>
    /// <param name="set">The advertising set used to manage the advertising operation.</param>
    /// <param name="parameters">The parameters to be set</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    public static async Task SetAdvertisingParametersAndRestartAsync(
        this IAdvertisingSet set,
        AdvertisingParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(set);
        bool isEnabled = set.IsAdvertising;
        if (isEnabled)
        {
            bool stopResult = await set
                .Broadcaster.StopAdvertisingAsync([set], cancellationToken)
                .ConfigureAwait(false);
            if (!stopResult)
            {
                throw new BleBroadcasterException(
                    set.Broadcaster,
                    "Could not set advertising parameters, set was already advertising but could not be stopped"
                );
            }
        }

        await set.SetAdvertisingParametersAsync(parameters, cancellationToken).ConfigureAwait(false);

        if (isEnabled)
        {
            await set.StartAdvertisingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
