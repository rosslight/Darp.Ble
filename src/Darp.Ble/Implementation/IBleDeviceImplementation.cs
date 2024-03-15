using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Implementation;

/// <summary>
/// 
/// </summary>
public interface IBleDeviceImplementation
{
    Task<InitializeResult> InitializeAsync();
    IBleObserverImplementation Observer { get; set; }
}

public interface IBleObserverImplementation
{
    bool TryStartScan([NotNullWhen(true)] out IObservable<IGapAdvertisement>? observable);
    void StopScan();
}