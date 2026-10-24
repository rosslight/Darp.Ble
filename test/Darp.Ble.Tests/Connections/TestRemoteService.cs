using Darp.Ble.Backends;
using Darp.Ble.Data;

namespace Darp.Ble.Tests.Connections;

internal sealed class TestRemoteService(BleUuid uuid, IRemoteCharacteristicDriver characteristic) : IRemoteServiceDriver
{
    private readonly IRemoteCharacteristicDriver _characteristic = characteristic;

    public BleUuid Uuid { get; } = uuid;

    public Task<IReadOnlyList<IRemoteCharacteristicDriver>> DiscoverCharacteristicsAsync(GattRequest request) =>
        Task.FromResult<IReadOnlyList<IRemoteCharacteristicDriver>>([_characteristic]);
}
