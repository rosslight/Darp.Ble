using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.WinRT.Connections;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT.Gap;

/// <summary>
/// Advertising on Windows. A non-connectable set is an advertisement publisher with the app's data; a connectable
/// set advertises the published services, as Windows connects centrals only to those. Windows chooses intervals and
/// addresses, sends no scan response data of the app's own, and keeps advertising when a central connects, so
/// sets are never stopped by a connection
/// </summary>
internal sealed class WinRTBroadcasterDriver(WinRTPeripheralDriver? peripheral, ILogger logger) : IBroadcasterDriver
{
    private readonly WinRTPeripheralDriver? _peripheral = peripheral;
    private readonly ILogger _logger = logger;

    public Task<IAdvertisingSetDriver> CreateAdvertisingSetAsync(
        AdvertisingParameters parameters,
        AdvertisingData data,
        AdvertisingData? scanResponseData,
        IAdvertisingSetSink sink,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (scanResponseData is not null)
            throw new NotSupportedException("Windows sends no scan response data of the app's own");
        return Task.FromResult<IAdvertisingSetDriver>(
            new WinRTAdvertisingSetDriver(_peripheral, parameters, data, _logger)
        );
    }
}
