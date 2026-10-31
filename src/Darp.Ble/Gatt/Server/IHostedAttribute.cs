using Darp.Ble.Backends;
using Darp.Ble.Connections;

namespace Darp.Ble.Gatt;

/// <summary> A hosted characteristic or descriptor, answering centrals through its handlers </summary>
internal interface IHostedAttribute
{
    GattAttributeDefinition Definition { get; }

    ValueTask<byte[]> ReadAsync(ConnectedCentral central, CancellationToken cancellationToken);

    ValueTask<GattProtocolStatus> WriteAsync(
        ConnectedCentral central,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    );
}
