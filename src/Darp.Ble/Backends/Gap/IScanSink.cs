using Darp.Ble.Gap;

namespace Darp.Ble.Backends;

/// <summary> Receives the reports of one radio scan. The core keeps every call cheap: it never blocks and never runs app code </summary>
public interface IScanSink
{
    /// <summary> Reports an advertisement; may be called on the driver's read loop </summary>
    /// <param name="advertisement"> The received advertisement </param>
    void OnReport(IGapAdvertisement advertisement);

    /// <summary> Reports that the radio stopped scanning because of an error. Scans see their queued reports first, then the error </summary>
    /// <param name="error"> What went wrong </param>
    void OnFailed(BleException error);
}
