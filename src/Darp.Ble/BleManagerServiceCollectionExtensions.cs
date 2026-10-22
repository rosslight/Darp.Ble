using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Darp.Ble;

/// <summary> Registers the <see cref="BleManager"/> in a service collection </summary>
public static class BleManagerServiceCollectionExtensions
{
    /// <summary> Registers a singleton <see cref="BleManager"/> </summary>
    /// <param name="services"> The service collection </param>
    /// <param name="configure"> Adds backends, e.g. <c>builder => builder.AddMock()</c> </param>
    /// <returns> The service collection </returns>
    /// <remarks> The manager uses the container's <see cref="ILoggerFactory"/> and <see cref="TimeProvider"/> when they are registered </remarks>
    public static IServiceCollection AddBleManager(
        this IServiceCollection services,
        Action<BleManagerBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        return services.AddSingleton(provider => new BleManager(
            configure,
            provider.GetService<ILoggerFactory>(),
            provider.GetService<TimeProvider>()
        ));
    }
}
