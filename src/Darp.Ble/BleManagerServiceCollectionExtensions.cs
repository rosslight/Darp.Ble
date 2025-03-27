using Microsoft.Extensions.DependencyInjection;

namespace Darp.Ble;

/// <summary> Extensions setting up the BleManager with DI </summary>
public static class BleManagerServiceCollectionExtensions
{
    /// <summary> Add a ble manager to the DI container and provide an action to configure it </summary>
    /// <param name="serviceCollection"> The service collection to add to </param>
    /// <param name="configure"> The callback to configure the BleManager </param>
    /// <returns> The <see cref="IServiceCollection"/> so that additional calls can be chained. </returns>
    public static IServiceCollection AddBleManager(
        this IServiceCollection serviceCollection,
        Action<BleManagerBuilder> configure
    )
    {
        return serviceCollection.AddBleManager((_, builder) => configure(builder));
    }

    /// <summary> Add a ble manager to the DI container and provide an action to configure it with an additional IServiceProvider </summary>
    /// <param name="serviceCollection"> The service collection to add to </param>
    /// <param name="configure"> The callback to configure the BleManager </param>
    /// <returns> The <see cref="IServiceCollection"/> so that additional calls can be chained. </returns>
    public static IServiceCollection AddBleManager(
        this IServiceCollection serviceCollection,
        Action<IServiceProvider, BleManagerBuilder> configure
    )
    {
        return serviceCollection.AddSingleton(provider =>
        {
            var builder = new BleManagerBuilder(provider);
            configure.Invoke(provider, builder);
            return builder.CreateManager();
        });
    }
}
