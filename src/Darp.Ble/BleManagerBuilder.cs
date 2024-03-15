using Darp.Ble.Implementation;

namespace Darp.Ble;

public sealed class BleManagerBuilder
{
    private readonly List<IBleImplementation> _implementations = [];

    public BleManagerBuilder WithImplementation<TImplementation>(Action<TImplementation>? config = null)
        where TImplementation : IBleImplementation, new()
    {
        var impl = new TImplementation();
        config?.Invoke(impl);
        _implementations.Add(impl);
        return this;
    }

    public BleManager CreateManager()
    {
        return new BleManager(_implementations);
    }
}