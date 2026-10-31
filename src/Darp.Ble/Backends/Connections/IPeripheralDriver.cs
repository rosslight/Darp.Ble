namespace Darp.Ble.Backends;

/// <summary> The peripheral role of an adapter: accepts connections from centrals </summary>
public interface IPeripheralDriver
{
    /// <summary> Binds the sink that receives connections. The core calls it once, when the adapter opens, before anything else </summary>
    /// <param name="sink"> The sink </param>
    void Attach(IGattServerSink sink);

    /// <summary> Adds a complete service </summary>
    /// <param name="service"> The service </param>
    /// <param name="cancellationToken"> Cancels adding </param>
    /// <returns> A task that completes once centrals can discover the service </returns>
    Task AddServiceAsync(GattServiceDefinition service, CancellationToken cancellationToken);

    /// <summary> Ends the connection of a central </summary>
    /// <param name="central"> The connection reported to the sink </param>
    /// <param name="cancellationToken"> Cancels waiting for the link to go down </param>
    /// <returns> A task that completes once the link is down </returns>
    Task DisconnectAsync(ICentralConnectionDriver central, CancellationToken cancellationToken);
}
