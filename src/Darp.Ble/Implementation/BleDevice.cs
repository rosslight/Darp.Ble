using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Logger;

namespace Darp.Ble.Implementation;

/// <inheritdoc />
public abstract class BleDevice : IBleDevice
{
    /// <summary> The logger </summary>
    protected IObserver<LogEvent>? Logger { get; }
    private bool _isInitializing;
    private IBleObserver? _bleObserver;
    private IBleCentral? _bleCentral;
    private IBleBroadcaster? _bleBroadcaster;
    private IBlePeripheral? _blePeripheral;

    /// <summary> Initialize a new ble device </summary>
    /// <param name="logger"> The logger </param>
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
    public IBleBroadcaster Broadcaster
    {
        get => ThrowIfNull(_bleBroadcaster);
        protected set => _bleBroadcaster = value;
    }
    /// <inheritdoc />
    public IBlePeripheral Peripheral
    {
        get => ThrowIfNull(_blePeripheral);
        protected set => _blePeripheral = value;
    }

    /// <inheritdoc />
    public async Task<InitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized) return InitializeResult.Success;
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
        DisposeCore();
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (Capabilities.HasFlag(Capabilities.Observer))
            await Observer.DisposeAsync();
        if (Capabilities.HasFlag(Capabilities.Central))
            await Central.DisposeAsync();
        if (Capabilities.HasFlag(Capabilities.Broadcaster))
            await Broadcaster.DisposeAsync();
        if (Capabilities.HasFlag(Capabilities.Peripheral))
            await Peripheral.DisposeAsync();
    }
    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void DisposeCore() { }
}