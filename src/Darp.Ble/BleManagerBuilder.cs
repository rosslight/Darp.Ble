using Darp.Ble.Implementation;
using Darp.Ble.Logger;

namespace Darp.Ble;

/// <summary> Configure the ble manager. Add new implementations or specify logging behavior </summary>
public sealed class BleManagerBuilder
{
    private readonly List<IBleImplementation> _implementations = [];
    private readonly List<Action<BleDevice, LogEvent>> _logActions = [];

    /// <summary> Add a new implementation </summary>
    /// <param name="config"> An optional callback to modify the implementation config </param>
    /// <typeparam name="TImplementation"> The type of the implementation </typeparam>
    /// <returns> The current builder </returns>
    public BleManagerBuilder WithImplementation<TImplementation>(Action<TImplementation>? config = null)
        where TImplementation : IBleImplementation, new()
    {
        var impl = new TImplementation();
        config?.Invoke(impl);
        _implementations.Add(impl);
        return this;
    }

    public BleManagerBuilder OnLog(Action<BleDevice, LogEvent> onLog)
    {
        _logActions.Add(onLog);
        return this;
    }

    /// <summary> Create a new ble manager </summary>
    /// <returns> The new ble manager </returns>
    public BleManager CreateManager()
    {
        return new BleManager(_implementations, _logActions);
    }
}