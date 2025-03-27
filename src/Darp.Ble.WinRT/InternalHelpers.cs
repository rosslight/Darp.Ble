using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Darp.Ble.WinRT;

internal static class InternalHelpers
{
    public static ILogger<T> GetLogger<T>(this IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<ILogger<T>>() ?? NullLogger<T>.Instance;
    }
}
