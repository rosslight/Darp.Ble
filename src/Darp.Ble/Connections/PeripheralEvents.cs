using Darp.Ble.Backends;

namespace Darp.Ble.Connections;

/// <summary> The sink of a peripheral role; keeps the sink methods off the public peripheral </summary>
internal sealed class PeripheralEvents(BlePeripheral peripheral) : IGattServerSink
{
    private readonly BlePeripheral _peripheral = peripheral;

    public void OnCentralConnected(ICentralConnectionDriver central) => _peripheral.OnCentralConnected(central);

    public void OnCentralDisconnected(ICentralConnectionDriver central, DisconnectInfo info) =>
        _peripheral.OnCentralDisconnected(central, info);

    public Task<byte[]> OnReadRequest(ICentralConnectionDriver central, GattAttributeDefinition attribute) =>
        _peripheral.OnReadRequestAsync(central, attribute);

    public void OnWriteCommand(
        ICentralConnectionDriver central,
        GattAttributeDefinition attribute,
        ReadOnlyMemory<byte> value
    ) => _peripheral.OnWriteCommand(central, attribute, value.ToArray());

    public Task OnWriteRequest(
        ICentralConnectionDriver central,
        GattAttributeDefinition attribute,
        ReadOnlyMemory<byte> value
    ) => _peripheral.OnWriteRequestAsync(central, attribute, value.ToArray());
}
