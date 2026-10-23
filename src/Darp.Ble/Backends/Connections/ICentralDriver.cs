using Darp.Ble.Connections;
using Darp.Ble.Data;

namespace Darp.Ble.Backends;

/// <summary> The central role of an adapter: connects to peripherals </summary>
public interface ICentralDriver
{
    /// <summary> Connects to a peripheral </summary>
    /// <param name="address"> The address of the peripheral </param>
    /// <param name="parameters"> The validated connection parameters </param>
    /// <param name="sink"> Receives the events of this connection; may be called before this method returns </param>
    /// <param name="cancellationToken"> Cancels connecting </param>
    /// <returns> The connection once the link is up; the core owns it from here on </returns>
    /// <exception cref="BleConnectException"> The peripheral could not be connected </exception>
    Task<IConnectionDriver> ConnectAsync(
        BleAddress address,
        BleConnectionParameters parameters,
        IConnectionSink sink,
        CancellationToken cancellationToken
    );
}
