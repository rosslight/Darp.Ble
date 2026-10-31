using Darp.Ble.Backends;

namespace Darp.Ble.Gap;

/// <summary> The sink of one advertising set; exists before the set, which only starts after it is attached </summary>
internal sealed class AdvertisingSetEvents : IAdvertisingSetSink
{
    private AdvertisingSet? _set;

    public void Attach(AdvertisingSet set) => Volatile.Write(ref _set, set);

    public void OnStoppedByConnection(ICentralConnectionDriver central) =>
        Volatile.Read(ref _set)?.OnStoppedByConnection(central);
}
