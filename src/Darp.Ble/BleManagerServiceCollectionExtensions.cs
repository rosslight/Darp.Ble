using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Darp.Ble;

/// <summary> Extensions setting up the BleManager with DI </summary>
public static class BleManagerServiceCollectionExtensions
{
    /// <summary> Add a ble manager to the DI container and provide an action to configure it </summary>
    /// <param name="services"> The service collection to add to </param>
    /// <param name="configure"> The callback to configure the BleManager </param>
    /// <returns> The <see cref="IServiceCollection"/> so that additional calls can be chained. </returns>
    public static IServiceCollection AddBleManager(
        this IServiceCollection services,
        Action<BleManagerBuilder> configure
    )
    {
        return services.AddBleManager((_, builder) => configure(builder));
    }

    /// <summary> Add a ble manager to the DI container and provide an action to configure it with an additional IServiceProvider </summary>
    /// <param name="services"> The service collection to add to </param>
    /// <param name="configure"> The callback to configure the BleManager </param>
    /// <returns> The <see cref="IServiceCollection"/> so that additional calls can be chained. </returns>
    public static IServiceCollection AddBleManager(
        this IServiceCollection services,
        Action<IServiceProvider, BleManagerBuilder> configure
    )
    {
        // Set defaults for logging if not already set
        services.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        return services.AddSingleton(provider =>
        {
            var builder = new BleManagerBuilder(provider);
            configure.Invoke(provider, builder);
            return builder.CreateManager();
        });
    }
}
