using Darp.Ble.Backends;

namespace Darp.Ble;

/// <summary> Collects the backends of a <see cref="BleManager"/> </summary>
public sealed class BleManagerBuilder
{
    private readonly List<IBleBackend> _backends = [];

    internal BleManagerBuilder() { }

    /// <summary> Adds a backend. Backend packages call this from their extensions, e.g. <c>AddMock()</c> </summary>
    /// <param name="backend"> The backend to add; adapters are enumerated in the order backends were added </param>
    /// <returns> This builder </returns>
    public BleManagerBuilder AddBackend(IBleBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);
        _backends.Add(backend);
        return this;
    }

    internal IBleBackend[] Build() => [.. _backends];
}
