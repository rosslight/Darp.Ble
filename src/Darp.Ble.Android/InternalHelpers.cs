using Darp.Ble.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Darp.Ble.Android;

internal static class InternalHelpers
{
    public static ILogger<T> GetLogger<T>(this IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<ILogger<T>>() ?? NullLogger<T>.Instance;
    }

    public static BleAddress ParseBleAddress(string? addressString)
    {
        return addressString is not null ? BleAddress.Parse(addressString, provider: null) : BleAddress.NotAvailable;
    }
}
