namespace Darp.Ble.Gatt.Services;

/// <summary> An echo service the adapter hosts </summary>
/// <param name="Write"> The characteristic requests are written to </param>
/// <param name="Notify"> The characteristic replies are notified on </param>
public sealed record LocalEchoService(
    LocalCharacteristic<Properties.Write> Write,
    LocalCharacteristic<Properties.Notify> Notify
);
