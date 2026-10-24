using Darp.Ble.Backends;
using Darp.Ble.Connections;

namespace Darp.Ble.Gatt;

/// <summary> The services the adapter hosts for centrals </summary>
public sealed class GattServer
{
    private readonly IPeripheralDriver _driver;
    private readonly OperationTracker _operations;

    internal GattServer(IPeripheralDriver driver, OperationTracker operations)
    {
        _driver = driver;
        _operations = operations;
    }

    /// <summary> Publishes a complete service </summary>
    /// <param name="service"> The service. It cannot change or publish again afterwards, even if publishing fails </param>
    /// <param name="cancellationToken"> Cancels publishing </param>
    /// <returns> The service, once centrals can discover it </returns>
    /// <exception cref="InvalidOperationException"> The builder already published a service </exception>
    public async Task<LocalGattService> AddServiceAsync(
        LocalGattServiceBuilder service,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(service);
        _operations.Enter(typeof(BlePeripheral));
        try
        {
            GattServiceDefinition definition = service.Publish();
            await _driver.AddServiceAsync(definition, cancellationToken).ConfigureAwait(false);
            return new LocalGattService(definition.Uuid);
        }
        finally
        {
            _operations.Exit();
        }
    }
}
