namespace Darp.Ble.Backends;

/// <summary> The peripheral role of an adapter: accepts connections from centrals </summary>
public interface IPeripheralDriver
{
    /// <summary> Binds the sink that receives connections. The core calls it once, when the adapter opens, before anything else </summary>
    /// <param name="sink"> The sink </param>
    void Attach(IGattServerSink sink);

    /// <summary> Publishes a complete service </summary>
    /// <param name="service"> The service </param>
    /// <param name="cancellationToken"> Cancels publishing </param>
    /// <returns> A task that completes once centrals can discover the service </returns>
    Task AddServiceAsync(GattServiceDefinition service, CancellationToken cancellationToken);

    /// <summary> Sends a notification to a central, without an answer </summary>
    /// <param name="central"> The connection of the central </param>
    /// <param name="characteristic"> The characteristic, as published </param>
    /// <param name="value"> The value; the driver takes what it needs before the task completes </param>
    /// <param name="cancellationToken"> Honoured while waiting to send, e.g. for buffer space; never once the backend accepted the packet </param>
    /// <returns> A task that completes once the backend accepted the notification; not when the central got it </returns>
    ValueTask NotifyAsync(
        ICentralConnectionDriver central,
        GattCharacteristicDefinition characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    );

    /// <summary> Ends the connection of a central </summary>
    /// <param name="central"> The connection reported to the sink </param>
    /// <param name="cancellationToken"> Cancels waiting for the link to go down </param>
    /// <returns> A task that completes once the link is down </returns>
    Task DisconnectAsync(ICentralConnectionDriver central, CancellationToken cancellationToken);
}
