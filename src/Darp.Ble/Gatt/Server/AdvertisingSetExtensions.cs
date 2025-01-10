using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Gatt.Server;

/// <summary> Extensions of <see cref="IAdvertisingSet"/> </summary>
public static class AdvertisingSetExtensions
{
    /// <summary> Start advertising using a legacy pdu. Under the hood, advertising sets are used </summary>
    /// <param name="broadcaster"> The broadcaster to advertise the advertisements </param>
    /// <param name="type"> The type of the advertisements to be advertised </param>
    /// <param name="peerAddress"> An optional address to whom the advertisements will be directed to </param>
    /// <param name="data"> The data to be advertised </param>
    /// <param name="scanResponseData"> The data to return on scan responses </param>
    /// <param name="interval"> The interval to advertise in </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> An async disposable to stop the broadcast </returns>
    public static async Task<IAsyncDisposable> AdvertiseAsync(this IBleBroadcaster broadcaster,
        BleEventType type = BleEventType.AdvInd,
        BleAddress? peerAddress = null,
        AdvertisingData? data = null,
        AdvertisingData? scanResponseData = null,
        ScanTiming interval = ScanTiming.Ms1000,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(broadcaster);
        var parameters = new AdvertisingParameters
        {
            Type = type,
            PeerAddress = peerAddress,
            MinPrimaryAdvertisingInterval = interval,
            MaxPrimaryAdvertisingInterval = interval,
        };
        IAdvertisingSet set = await broadcaster.CreateAdvertisingSetAsync(
            parameters,
            data,
            scanResponseData,
            cancellationToken)
            .ConfigureAwait(false);
        return await set.StartAdvertisingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary> Start advertising a specific advertising set </summary>
    /// <param name="broadcaster"> The broadcaster to advertise the sets for </param>
    /// <param name="set"> The advertising set to broadcast </param>
    /// <param name="sets"> Additional advertising sets </param>
    /// <returns></returns>
    public static Task<IAsyncDisposable> StartAdvertisingAsync(this IBleBroadcaster broadcaster,
        IAdvertisingSet set,
        params IEnumerable<IAdvertisingSet> sets)
    {
        ArgumentNullException.ThrowIfNull(broadcaster);

        (IAdvertisingSet, TimeSpan, byte)[] advertisingSetStartInfo = sets
            .Prepend(set)
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
    public static Task<IAsyncDisposable> StartAdvertisingAsync(this IAdvertisingSet set,
        TimeSpan duration = default,
        byte numberOfEvents = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(set);
        return set.Broadcaster.StartAdvertisingAsync([(set, duration, numberOfEvents)], cancellationToken);
    }
}