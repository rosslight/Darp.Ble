using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Logger;

namespace Darp.Ble;

public interface IBleDevice : IAsyncDisposable
{
    
}

/// <summary> The base interface of a ble device. </summary>
public abstract class BleDevice : IBleDevice
{
    protected readonly IObserver<LogEvent>? Logger;
    private bool _isInitializing;
    private BleObserver? _bleObserver;
    private IBleCentral? _bleCentral;
    private BlePeripheral? _blePeripheral;

    protected BleDevice(IObserver<(BleDevice, LogEvent)>? logger)
    {
        if (logger is not null) Logger = System.Reactive.Observer.Create<LogEvent>(x => logger.OnNext((this, x)));
    }

    /// <summary> True if the device was successfully initialized </summary>
    public bool IsInitialized { get; private set; }

    /// <summary> Get an implementation specific identification string </summary>
    public abstract string Identifier { get; }

    /// <summary> An optional name </summary>
    public abstract string? Name { get; }

    /// <summary> Initializes the ble device </summary>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> Success or a custom error code </returns>
    public async Task<InitializeResult> InitializeAsync(CancellationToken cancellationToken)
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

    /// <summary>
    /// Gives back capabilities of this device. Before the device was successfully initialized, the capabilities are unknown
    /// </summary>
    public Capabilities Capabilities => Capabilities.None
                                        | (_bleObserver is not null ? Capabilities.Observer : Capabilities.None);

    /// <summary> Returns a view of the device in Observer Role </summary>
    /// <exception cref="NotInitializedException"> Thrown when the device has not been initialized </exception>
    /// <exception cref="NotSupportedException"> Thrown when the role is not supported </exception>
    public BleObserver Observer
    {
        get => ThrowIfNull(_bleObserver);
        protected set => _bleObserver = value;
    }
    /// <summary> Returns a view of the device in Central Role </summary>
    /// <exception cref="NotInitializedException"> Thrown when the device has not been initialized </exception>
    /// <exception cref="NotSupportedException"> Thrown when the role is not supported </exception>
    public IBleCentral Central
    {
        get => ThrowIfNull(_bleCentral);
        protected set => _bleCentral = value;
    }
    /// <summary> Returns a view of the device in Peripheral Role </summary>
    /// <exception cref="NotInitializedException"> Thrown when the device has not been initialized </exception>
    /// <exception cref="NotSupportedException"> Thrown when the role is not supported </exception>
    public BlePeripheral Peripheral
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