using Darp.Ble.Data;
using Darp.Ble.Implementation;

namespace Darp.Ble.Exceptions;

/// <summary> Represents an error thrown when post-connect initialization failed while connecting to a peripheral </summary>
public sealed class BleCentralConnectionInitializationFailedException : BleCentralConnectionFailedException
{
    /// <summary> Initialize a new exception when post-connect initialization failed </summary>
    /// <param name="central"> The central responsible for the connection </param>
    /// <param name="address"> The address of the peer that was being connected </param>
    /// <param name="stage"> The initialization stage that failed </param>
    /// <param name="connectionHandle"> The connection handle, if known </param>
    /// <param name="innerException"> The underlying failure </param>
    public BleCentralConnectionInitializationFailedException(
        BleCentral central,
        BleAddress address,
        ushort? connectionHandle,
        Exception innerException
    )
        : base(central, $"Connection to {address} failed during initialization.", innerException)
    {
        Address = address;
        ConnectionHandle = connectionHandle;
    }

    /// <summary> The address of the peer that was being connected </summary>
    public BleAddress Address { get; }

    /// <summary> The connection handle, if it was known when the failure occurred </summary>
    public ushort? ConnectionHandle { get; }
}
