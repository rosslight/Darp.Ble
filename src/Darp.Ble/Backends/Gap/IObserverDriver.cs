using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Backends;

/// <summary> The scanning of an adapter. The core runs at most one radio scan at a time and shares it between all scans of the app </summary>
public interface IObserverDriver
{
    /// <summary> Starts scanning and reports every advertisement to the sink until stopped </summary>
    /// <param name="parameters"> The radio settings; the driver rounds the timings to the controller's units </param>
    /// <param name="sink"> Receives the reports of this run; a new run gets a new sink </param>
    /// <param name="cancellationToken"> Cancels starting </param>
    /// <returns> A task that completes once the radio scans </returns>
    Task StartScanAsync(BleScanParameters parameters, IScanSink sink, CancellationToken cancellationToken);

    /// <summary> Stops the running scan. Must also succeed when the radio already stopped, e.g. after <see cref="IScanSink.OnFailed"/> </summary>
    /// <param name="cancellationToken"> Cancels stopping </param>
    /// <returns> A task that completes once the radio stopped scanning </returns>
    Task StopScanAsync(CancellationToken cancellationToken);
}
