using System.Runtime.CompilerServices;
using Darp.Ble.Data;

namespace Darp.Ble.Gap;

/// <summary> Filters for streams of advertisements, e.g. <see cref="BleObserver.ScanAsync(CancellationToken)"/> </summary>
public static class AdvertisementStreamExtensions
{
    /// <summary> Only advertisements from this address; all of them when the address is null </summary>
    /// <param name="source"> The advertisements </param>
    /// <param name="address"> The address to keep; without its type (<see cref="BleAddressType.NotAvailable"/>), of any type </param>
    /// <typeparam name="TAdv"> The type of the advertisements </typeparam>
    /// <returns> The filtered advertisements </returns>
    public static IAsyncEnumerable<TAdv> WhereAddress<TAdv>(this IAsyncEnumerable<TAdv> source, BleAddress? address)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        if (address is null)
            return source;
        // An address without its type, e.g. parsed from text, matches its value of any type
        return address.Type is BleAddressType.NotAvailable
            ? source.Where(adv => adv.Address.Value == address.Value)
            : source.Where(adv => adv.Address.Equals(address));
    }

    /// <summary> Only advertisements from this address value; all of them when the address is null </summary>
    /// <param name="source"> The advertisements </param>
    /// <param name="address"> The 48-bit address value to keep, of any address type </param>
    /// <typeparam name="TAdv"> The type of the advertisements </typeparam>
    /// <returns> The filtered advertisements </returns>
    public static IAsyncEnumerable<TAdv> WhereAddress<TAdv>(this IAsyncEnumerable<TAdv> source, ulong? address)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return address is not { } value ? source : source.Where(adv => adv.Address.Equals(value));
    }

    /// <summary> Only advertisements of this event type </summary>
    /// <param name="source"> The advertisements </param>
    /// <param name="type"> The event type to keep </param>
    /// <typeparam name="TAdv"> The type of the advertisements </typeparam>
    /// <returns> The filtered advertisements </returns>
    public static IAsyncEnumerable<TAdv> WhereType<TAdv>(this IAsyncEnumerable<TAdv> source, BleEventType type)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(adv => adv.EventType == type);
    }

    /// <summary> Only connectable advertisements </summary>
    /// <param name="source"> The advertisements </param>
    /// <typeparam name="TAdv"> The type of the advertisements </typeparam>
    /// <returns> The filtered advertisements </returns>
    public static IAsyncEnumerable<TAdv> WhereConnectable<TAdv>(this IAsyncEnumerable<TAdv> source)
        where TAdv : IGapAdvertisement => source.WhereEventFlag(BleEventType.Connectable);

    /// <summary> Only directed advertisements </summary>
    /// <param name="source"> The advertisements </param>
    /// <typeparam name="TAdv"> The type of the advertisements </typeparam>
    /// <returns> The filtered advertisements </returns>
    public static IAsyncEnumerable<TAdv> WhereDirected<TAdv>(this IAsyncEnumerable<TAdv> source)
        where TAdv : IGapAdvertisement => source.WhereEventFlag(BleEventType.Directed);

    /// <summary> Only scannable advertisements </summary>
    /// <param name="source"> The advertisements </param>
    /// <typeparam name="TAdv"> The type of the advertisements </typeparam>
    /// <returns> The filtered advertisements </returns>
    public static IAsyncEnumerable<TAdv> WhereScannable<TAdv>(this IAsyncEnumerable<TAdv> source)
        where TAdv : IGapAdvertisement => source.WhereEventFlag(BleEventType.Scannable);

    /// <summary> Only scan responses </summary>
    /// <param name="source"> The advertisements </param>
    /// <typeparam name="TAdv"> The type of the advertisements </typeparam>
    /// <returns> The filtered advertisements </returns>
    public static IAsyncEnumerable<TAdv> WhereScanResponse<TAdv>(this IAsyncEnumerable<TAdv> source)
        where TAdv : IGapAdvertisement => source.WhereEventFlag(BleEventType.ScanResponse);

    /// <summary> Only advertisements that list this service; all of them when the service is null </summary>
    /// <param name="source"> The advertisements </param>
    /// <param name="service"> The service uuid to look for </param>
    /// <typeparam name="TAdv"> The type of the advertisements </typeparam>
    /// <returns> The filtered advertisements </returns>
    public static IAsyncEnumerable<TAdv> WhereService<TAdv>(this IAsyncEnumerable<TAdv> source, BleUuid? service)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return service is not { } uuid
            ? source
            : source.Where(adv => adv.Data.GetServiceUuids().Any(listed => listed.Equals(uuid)));
    }

    /// <summary> Only advertisements that list this 16-bit service; all of them when the service is null </summary>
    /// <param name="source"> The advertisements </param>
    /// <param name="service"> The 16-bit service uuid to look for </param>
    /// <typeparam name="TAdv"> The type of the advertisements </typeparam>
    /// <returns> The filtered advertisements </returns>
    public static IAsyncEnumerable<TAdv> WhereService<TAdv>(this IAsyncEnumerable<TAdv> source, ushort? service)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return service is not { } uuid
            ? source
            : source.Where(adv => adv.Data.GetServiceUuids().Any(listed => listed.Equals(uuid)));
    }

    /// <summary> Only advertisements whose user data is a <typeparamref name="TUserData"/>; others, and null user data, are skipped </summary>
    /// <param name="source"> The advertisements </param>
    /// <typeparam name="TUserData"> The type of the user data </typeparam>
    /// <returns> The advertisements with their user data typed </returns>
    public static IAsyncEnumerable<IGapAdvertisement<TUserData>> OfUserData<TUserData>(
        this IAsyncEnumerable<IGapAdvertisement> source
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        return OfUserDataCore<TUserData>(source, CancellationToken.None);
    }

    /// <summary> The advertisements with their user data typed </summary>
    /// <param name="source"> The advertisements </param>
    /// <typeparam name="TUserData"> The type of the user data </typeparam>
    /// <returns> The advertisements with their user data typed </returns>
    /// <exception cref="InvalidCastException"> An advertisement has no user data of this type, or its user data is null </exception>
    public static IAsyncEnumerable<IGapAdvertisement<TUserData>> CastUserData<TUserData>(
        this IAsyncEnumerable<IGapAdvertisement> source
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(advertisement =>
            TryGetTyped(advertisement, out IGapAdvertisement<TUserData>? typed)
                ? typed
                : throw new InvalidCastException(
                    $"Cannot cast UserData on '{advertisement.GetType().Name}' to '{typeof(TUserData).Name}'."
                )
        );
    }

    private static async IAsyncEnumerable<IGapAdvertisement<TUserData>> OfUserDataCore<TUserData>(
        IAsyncEnumerable<IGapAdvertisement> source,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await foreach (
            IGapAdvertisement advertisement in source.WithCancellation(cancellationToken).ConfigureAwait(false)
        )
        {
            if (TryGetTyped(advertisement, out IGapAdvertisement<TUserData>? typed))
                yield return typed;
        }
    }

    private static IAsyncEnumerable<TAdv> WhereEventFlag<TAdv>(this IAsyncEnumerable<TAdv> source, BleEventType flag)
        where TAdv : IGapAdvertisement
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(adv => adv.EventType.HasFlag(flag));
    }

    private static bool TryGetTyped<TUserData>(
        IGapAdvertisement advertisement,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IGapAdvertisement<TUserData>? typed
    )
    {
        switch (advertisement)
        {
            case IGapAdvertisement<TUserData> { UserData: not null } alreadyTyped:
                typed = alreadyTyped;
                return true;
            case IGapAdvertisementWithUserData { UserData: TUserData data }:
                typed = advertisement.WithUserData(data);
                return true;
            default:
                typed = null;
                return false;
        }
    }
}
