namespace Darp.Ble;

/// <summary> The settings a <see cref="BleManager"/> gives every adapter it opens </summary>
/// <param name="AdvertisementQueue"> The queue of a scan that does not bring its own </param>
/// <param name="NotificationQueue"> The queue of a notification stream that does not bring its own </param>
internal sealed record BleCoreOptions(QueueOptions AdvertisementQueue, QueueOptions NotificationQueue);
