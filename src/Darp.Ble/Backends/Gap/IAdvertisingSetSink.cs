namespace Darp.Ble.Backends;

/// <summary> Receives what happens to an advertising set without the core asking. Called on the driver's thread; the core returns at once </summary>
public interface IAdvertisingSetSink
{
    /// <summary> A central connected through the set, so the set stopped advertising </summary>
    /// <remarks> Reported for the run the set advertises in, before a later <see cref="IAdvertisingSetDriver.StartAsync"/> or <see cref="IAdvertisingSetDriver.StopAsync"/> completes </remarks>
    /// <param name="central"> The connection, as reported to the peripheral role's <see cref="IGattServerSink.OnCentralConnected"/> before this call </param>
    void OnStoppedByConnection(ICentralConnectionDriver central);

    /// <summary> The backend stopped the set because of an error, e.g. the radio turned off; it stays stopped until it is started again </summary>
    /// <remarks> Reported for the run the set advertises in, before a later <see cref="IAdvertisingSetDriver.StartAsync"/> or <see cref="IAdvertisingSetDriver.StopAsync"/> completes </remarks>
    /// <param name="error"> Why the set stopped </param>
    void OnFailed(BleException error);
}
