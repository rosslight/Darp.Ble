using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Backends;

/// <summary> One advertising set. The core serializes the calls of a set and only changes parameters while it is stopped </summary>
[Experimental(Experiments.Backends)]
public interface IAdvertisingSetDriver : IAsyncDisposable
{
    /// <summary> The address the set advertises with </summary>
    BleAddress RandomAddress { get; }

    /// <summary> Starts advertising </summary>
    /// <param name="cancellationToken"> Cancels starting </param>
    /// <returns> A task that completes once the set advertises </returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary> Stops advertising </summary>
    /// <param name="cancellationToken"> Cancels stopping </param>
    /// <returns> A task that completes once the set stopped </returns>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary> Changes the parameters of the stopped set </summary>
    /// <param name="parameters"> The validated parameters </param>
    /// <param name="cancellationToken"> Cancels the change </param>
    /// <returns> A task that completes once the parameters apply </returns>
    Task SetParametersAsync(AdvertisingParameters parameters, CancellationToken cancellationToken);

    /// <summary> Changes the advertising data </summary>
    /// <param name="data"> The new data </param>
    /// <param name="cancellationToken"> Cancels the change </param>
    /// <returns> A task that completes once the data apply </returns>
    Task SetDataAsync(AdvertisingData data, CancellationToken cancellationToken);

    /// <summary> Changes the scan response data </summary>
    /// <param name="data"> The new data </param>
    /// <param name="cancellationToken"> Cancels the change </param>
    /// <returns> A task that completes once the data apply </returns>
    Task SetScanResponseDataAsync(AdvertisingData data, CancellationToken cancellationToken);

    /// <summary> Changes the address the set advertises with; only called when the adapter can set random addresses </summary>
    /// <param name="address"> The new address </param>
    /// <param name="cancellationToken"> Cancels the change </param>
    /// <returns> A task that completes once the address applies </returns>
    Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken);
}
