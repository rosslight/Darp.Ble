namespace Darp.Ble.WinRT.Gatt;

/// <summary>
/// Windows reports the subscribed clients of a characteristic as a whole list on every change, also for raw CCCD writes.
/// This turns those lists into who subscribed and who left, per characteristic
/// </summary>
internal sealed class SubscriptionSnapshots<TCharacteristic, TClient>
    where TCharacteristic : notnull
    where TClient : notnull
{
    private readonly Lock _lock = new();
    private readonly Dictionary<TCharacteristic, HashSet<TClient>> _subscribed = [];

    /// <summary> Takes the characteristic's current list; repeating it reports nothing </summary>
    public (IReadOnlyList<TClient> Added, IReadOnlyList<TClient> Removed) Update(
        TCharacteristic characteristic,
        IEnumerable<TClient> subscribed
    )
    {
        HashSet<TClient> current = [.. subscribed];
        lock (_lock)
        {
            HashSet<TClient> previous = _subscribed.GetValueOrDefault(characteristic) ?? [];
            TClient[] added = [.. current.Where(client => !previous.Contains(client))];
            TClient[] removed = [.. previous.Where(client => !current.Contains(client))];
            _subscribed[characteristic] = current;
            return (added, removed);
        }
    }

    public bool IsSubscribed(TCharacteristic characteristic, TClient client)
    {
        lock (_lock)
            return _subscribed.GetValueOrDefault(characteristic)?.Contains(client) is true;
    }

    /// <summary> The client's connection ended: it is subscribed nowhere, and a later list with it counts as new </summary>
    public void Forget(TClient client)
    {
        lock (_lock)
        {
            foreach (HashSet<TClient> clients in _subscribed.Values)
                clients.Remove(client);
        }
    }
}
