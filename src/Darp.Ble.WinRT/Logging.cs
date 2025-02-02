using Darp.Ble.Data.AssignedNumbers;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace Darp.Ble.WinRT;

internal static partial class Logging
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Ignoring data section {AdType}. This type is reserved by Windows"
    )]
    public static partial void LogIgnoreDataSectionReservedType(
        this ILogger logger,
        AdTypes adType
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Publisher status changed to {Status} with {Error}"
    )]
    public static partial void LogPublisherChangedToError(
        this ILogger logger,
        BluetoothLEAdvertisementPublisherStatus status,
        BluetoothError error
    );
}
