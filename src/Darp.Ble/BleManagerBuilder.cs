using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Darp.Ble;

/// <summary> Configure the ble manager. Add new implementations or specify logging behavior </summary>
public sealed class BleManagerBuilder(IServiceProvider? serviceProvider)
{
    private readonly List<IBleFactory> _factories = [];
    private ILoggerFactory? _loggerFactory;
    private readonly IServiceProvider? _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public BleManagerBuilder()
        : this(serviceProvider: null) { }

    /// <summary> Add a new factory </summary>
    /// <param name="config"> An optional callback to modify the implementation config </param>
    /// <typeparam name="TFactory"> The type of the factory </typeparam>
    /// <returns> The current builder </returns>
    public BleManagerBuilder Add<TFactory>(Action<TFactory>? config = null)
        where TFactory : IBleFactory, new()
    {
        var factory = new TFactory();
        config?.Invoke(factory);
        return Add(factory);
    }

    /// <summary> Add a new factory directly </summary>
    /// <param name="factory"> The factory to be added </param>
    /// <returns> The current builder  </returns>
    public BleManagerBuilder Add(IBleFactory factory)
    {
        _factories.Add(factory);
        return this;
    }

    /// <summary> Register a handler for log events </summary>
    /// <param name="loggerFactory"> The logger factory to be used to redirect logs of the ble library to </param>
    /// <returns> The current builder </returns>
    public BleManagerBuilder SetLogger(ILoggerFactory? loggerFactory)
    {
        if (_serviceProvider is not null)
            throw new InvalidOperationException(
                "The BleManagerBuilder was initialized with a service provider. Use the provider to initialize logging instead"
            );
        _loggerFactory = loggerFactory;
        return this;
    }

    /// <summary> Create a new ble manager </summary>
    /// <returns> The new ble manager </returns>
    public BleManager CreateManager()
    {
        return new BleManager(_factories, _serviceProvider ?? new BleManagerServiceProvider(_loggerFactory));
    }
}

file sealed class BleManagerServiceProvider(ILoggerFactory? loggerFactory) : IServiceProvider
{
    private readonly ILoggerFactory? _loggerFactory = loggerFactory;

    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "GenericTypeArgument has to be specified outside and will always be present"
    )]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Logger<>))]
    public object? GetService(Type serviceType)
    {
        // Ensure our calls for loggers are always handled correctly
        if (serviceType == typeof(ILoggerFactory))
            return _loggerFactory ?? NullLoggerFactory.Instance;
        if (serviceType == typeof(ILogger))
            return _loggerFactory?.CreateLogger("Darp.Ble") ?? NullLogger.Instance;
        if (serviceType.GetGenericTypeDefinition() == typeof(ILogger<>))
        {
            Type openGeneric = typeof(Logger<>);
            Type closedGeneric = openGeneric.MakeGenericType(serviceType.GenericTypeArguments[0]);
            return Activator.CreateInstance(closedGeneric, _loggerFactory ?? NullLoggerFactory.Instance);
        }

        return null;
    }
}
