namespace Darp.Ble.Gatt.Services;

/// <summary> An echo service the adapter hosts </summary>
/// <param name="Service"> The hosted service, e.g. to advertise its UUID </param>
/// <param name="Write"> The characteristic requests are written to </param>
/// <param name="Notify"> The characteristic replies are notified on </param>
public sealed record LocalEchoService(
    LocalGattService Service,
    LocalCharacteristic<Properties.Write> Write,
    LocalCharacteristic<Properties.Notify> Notify
);
