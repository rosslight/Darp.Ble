using Darp.Ble.Data;

namespace Darp.Ble.Backends;

/// <summary> The driver of an opened adapter. The core wraps it in a <see cref="BleAdapter"/> and disposes it once </summary>
public interface IBleAdapterDriver : IAsyncDisposable
{
    /// <summary> The roles and features this adapter supports. Each role flag must match whether its role driver exists </summary>
    Capabilities Capabilities { get; }

    /// <summary> The observer role, or null if the adapter cannot scan </summary>
    IObserverDriver? Observer { get; }

    /// <summary> The broadcaster role, or null if the adapter cannot advertise </summary>
    IBroadcasterDriver? Broadcaster { get; }

    /// <summary> The central role, or null if the adapter cannot connect to peripherals </summary>
    ICentralDriver? Central { get; }

    /// <summary> The peripheral role, or null if centrals cannot connect to the adapter </summary>
    IPeripheralDriver? Peripheral { get; }

    /// <summary> The adapter's own random address </summary>
    BleAddress RandomAddress { get; }

    /// <summary> Changes the adapter's own random address; only called with <see cref="Capabilities.SetRandomAddress"/> </summary>
    /// <param name="address"> The new address </param>
    /// <param name="cancellationToken"> Cancels the change </param>
    /// <returns> A task that completes once the address applies </returns>
    Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken);
}
