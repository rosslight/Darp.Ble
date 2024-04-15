using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Logger;

namespace Darp.Ble;

/// <summary> The base interface of a ble device. </summary>
public interface IBleDevice : IAsyncDisposable
{
    /// <summary> True if the device was successfully initialized </summary>
    public bool IsInitialized { get; }

    /// <summary> Get an implementation specific identification string </summary>
    public abstract string Identifier { get; }

    /// <summary> An optional name </summary>
    public abstract string? Name { get; }

    /// <summary>
    /// Gives back capabilities of this device. Before the device was successfully initialized, the capabilities are unknown
    /// </summary>
    Capabilities Capabilities { get; }

    /// <summary> Returns a view of the device in Observer Role </summary>
    /// <exception cref="NotInitializedException"> Thrown when the device has not been initialized </exception>
    /// <exception cref="NotSupportedException"> Thrown when the role is not supported </exception>
    IBleObserver Observer { get; }
    /// <summary> Returns a view of the device in Central Role </summary>
    /// <exception cref="NotInitializedException"> Thrown when the device has not been initialized </exception>
    /// <exception cref="NotSupportedException"> Thrown when the role is not supported </exception>
    IBleCentral Central { get; }
    /// <summary> Returns a view of the device in Peripheral Role </summary>
    /// <exception cref="NotInitializedException"> Thrown when the device has not been initialized </exception>
    /// <exception cref="NotSupportedException"> Thrown when the role is not supported </exception>
    IBlePeripheral Peripheral { get; }

    /// <summary> Initializes the ble device </summary>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> Success or a custom error code </returns>
    Task<InitializeResult> InitializeAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public abstract class BleDevice : IBleDevice
{
    protected readonly IObserver<LogEvent>? Logger;
    private bool _isInitializing;
    private IBleObserver? _bleObserver;
    private IBleCentral? _bleCentral;
    private IBlePeripheral? _blePeripheral;

    protected BleDevice(IObserver<(BleDevice, LogEvent)>? logger)
    {
        if (logger is not null) Logger = System.Reactive.Observer.Create<LogEvent>(x => logger.OnNext((this, x)));
    }

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <inheritdoc />
    public abstract string Identifier { get; }

    /// <inheritdoc />
    public abstract string? Name { get; }

    /// <inheritdoc />
    public async Task<InitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitializing) return InitializeResult.AlreadyInitializing;
        try
        {
            _isInitializing = true;
            InitializeResult result = await InitializeAsyncCore(cancellationToken);
            if (result is not InitializeResult.Success)
                return result;
            Logger?.Debug("Adapter Initialized!");
            IsInitialized = true;
            return InitializeResult.Success;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    /// <summary> Initializes the ble device. </summary>
    /// <param name="cancellationToken"></param>
    /// <returns> The status of the initialization. Success or a custom error code. </returns>
    protected abstract Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken);

    /// <inheritdoc />
    public Capabilities Capabilities => Capabilities.None
                                        | (_bleObserver is not null ? Capabilities.Observer : Capabilities.None);

    /// <inheritdoc />
    public IBleObserver Observer
    {
        get => ThrowIfNull(_bleObserver);
        protected set => _bleObserver = value;
    }
    /// <inheritdoc />
    public IBleCentral Central
    {
        get => ThrowIfNull(_bleCentral);
        protected set => _bleCentral = value;
    }
    /// <inheritdoc />
    public IBlePeripheral Peripheral
    {
        get => ThrowIfNull(_blePeripheral);
        protected set => _blePeripheral = value;
    }

    /// <summary> Throws if not initialized or null </summary>
    /// <exception cref="NotInitializedException"> Thrown when the device has not been initialized </exception>
    /// <exception cref="NotSupportedException"> Thrown when the role is not supported </exception>
    [return: NotNullIfNotNull(nameof(param))]
    private T ThrowIfNull<T>(T? param)
    {
        if(!IsInitialized) throw new NotInitializedException(this);
        if (param is not null) return param;
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        DisposeSyncInternal();
        await DisposeInternalAsync();
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeInternalAsync() => ValueTask.CompletedTask;
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void DisposeSyncInternal() { }
}