using Darp.Ble.Connections;
using Darp.Ble.Gatt;

namespace Darp.Ble.Backends;

/// <summary> Receives what centrals do with the adapter's peripheral role. Called on the driver's thread; the core returns at once </summary>
public interface IGattServerSink
{
    /// <summary> A central connected </summary>
    /// <param name="central"> The connection; the same object identifies it in later calls </param>
    void OnCentralConnected(ICentralConnectionDriver central);

    /// <summary> A connection of a central ended, for whatever reason, including a disconnect the core asked for </summary>
    /// <param name="central"> The connection reported by <see cref="OnCentralConnected"/> </param>
    /// <param name="info"> Why it ended </param>
    void OnCentralDisconnected(ICentralConnectionDriver central, DisconnectInfo info);

    /// <summary> A central turned notifications of a characteristic on or off, through its CCCD; per connection, reset when it reconnects </summary>
    /// <param name="central"> The connection of the central </param>
    /// <param name="characteristic"> The characteristic, as published </param>
    /// <param name="enabled"> Whether notifications are on now </param>
    void OnSubscriptionChanged(
        ICentralConnectionDriver central,
        GattCharacteristicDefinition characteristic,
        bool enabled
    );

    /// <summary> A central reads a hosted characteristic or descriptor. The core runs the app's handler off the driver's thread </summary>
    /// <param name="central"> The connection of the central </param>
    /// <param name="attribute"> The characteristic or descriptor, as published </param>
    /// <returns> The value to answer with. A <see cref="GattException"/> carries the ATT error to answer with instead </returns>
    Task<byte[]> OnReadRequest(ICentralConnectionDriver central, GattAttributeDefinition attribute);

    /// <summary> A central writes a hosted characteristic or descriptor with a write command, which gets no answer. The core copies the value and runs the handler on its own </summary>
    /// <param name="central"> The connection of the central </param>
    /// <param name="attribute"> The characteristic or descriptor, as published </param>
    /// <param name="value"> The value </param>
    void OnWriteCommand(
        ICentralConnectionDriver central,
        GattAttributeDefinition attribute,
        ReadOnlyMemory<byte> value
    );

    /// <summary> A central writes a hosted characteristic or descriptor with a write request. The core copies the value before it returns </summary>
    /// <param name="central"> The connection of the central </param>
    /// <param name="attribute"> The characteristic or descriptor, as published </param>
    /// <param name="value"> The complete value; the driver assembles prepared writes </param>
    /// <returns> A task that completes once the app accepted the value. A <see cref="GattException"/> carries the ATT error to answer with instead </returns>
    Task OnWriteRequest(
        ICentralConnectionDriver central,
        GattAttributeDefinition attribute,
        ReadOnlyMemory<byte> value
    );
}
