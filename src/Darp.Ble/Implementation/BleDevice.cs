using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Exceptions;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Implementation;

/// <inheritdoc />
/// <param name="logger"> The logger </param>
/// <param name="serviceProvider"> The service provider </param>
public abstract class BleDevice(IServiceProvider serviceProvider, ILogger<BleDevice> logger) : IBleDevice
{
    private bool _isInitializing;
    private IBleObserver? _bleObserver;
    private IBleCentral? _bleCentral;
    private IBleBroadcaster? _bleBroadcaster;
    private IBlePeripheral? _blePeripheral;

    /// <summary> The logger </summary>
    protected ILogger<BleDevice> Logger { get; } = logger;

    /// <summary> The logger factory </summary>
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    /// <inheritdoc />
    public bool IsInitialized { get; private set; }

    /// <summary> True, if the device started disposing or is already disposed </summary>
    internal bool IsDisposing { get; private set; }

    /// <inheritdoc />
    public bool IsDisposed { get; private set; }

    /// <inheritdoc />
    public abstract string Identifier { get; }

    /// <inheritdoc />
    public abstract string? Name { get; set; }

    /// <inheritdoc />
    public abstract AppearanceValues Appearance { get; set; }

    /// <inheritdoc />
    public Capabilities Capabilities =>
        Capabilities.None
        | (_bleObserver is not null ? Capabilities.Observer : Capabilities.None)
        | (_bleCentral is not null ? Capabilities.Central : Capabilities.None)
        | (_bleBroadcaster is not null ? Capabilities.Broadcaster : Capabilities.None)
        | (_blePeripheral is not null ? Capabilities.Peripheral : Capabilities.None);

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
    public abstract BleAddress RandomAddress { get; }

    /// <inheritdoc />
    public async Task<InitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return InitializeResult.Success;
        if (_isInitializing)
            return InitializeResult.AlreadyInitializing;
        try
        {
            _isInitializing = true;
            InitializeResult result = await InitializeAsyncCore(cancellationToken).ConfigureAwait(false);
            if (result is not InitializeResult.Success)
                return result;
            IsInitialized = true;
            Logger.LogBleDeviceInitialized(Name);
            return InitializeResult.Success;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    /// <inheritdoc />
    public Task SetRandomAddressAsync(BleAddress randomAddress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(randomAddress);
        return SetRandomAddressAsyncCore(randomAddress, cancellationToken);
    }

    /// <inheritdoc cref="SetRandomAddressAsync" />
    protected abstract Task SetRandomAddressAsyncCore(BleAddress randomAddress, CancellationToken cancellationToken);

    /// <summary> Initializes the ble device. </summary>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> The status of the initialization. Success or a custom error code. </returns>
    protected abstract Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken);

    /// <summary> Throws if not initialized or null </summary>
    /// <exception cref="NotInitializedException"> Thrown when the device has not been initialized </exception>
    /// <exception cref="NotSupportedException"> Thrown when the role is not supported </exception>
    [return: NotNullIfNotNull(nameof(param))]
    private T ThrowIfNull<T>(T? param, [CallerMemberName] string? roleName = null)
    {
        if (!IsInitialized && !_isInitializing)
            throw new NotInitializedException(this);
        if (param is not null)
            return param;
        throw new NotSupportedException($"The device does not support role {roleName}");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (IsDisposing || IsDisposed)
            return;
        IsDisposing = true;
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
        IsDisposed = true;
        Logger.LogBleDeviceDisposed(Name);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    /// <param name="disposing">
    /// True, when this method was called by the synchronous <see cref="IDisposable.Dispose"/> method;
    /// False if called by the asynchronous <see cref="IAsyncDisposable.DisposeAsync"/> method
    /// </param>
    protected virtual void Dispose(bool disposing) { }

    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (Capabilities.HasFlag(Capabilities.Observer) && Observer is BleObserver observer)
            await observer.DisposeAsync().ConfigureAwait(false);
        if (Capabilities.HasFlag(Capabilities.Central) && Central is BleCentral central)
            await central.DisposeAsync().ConfigureAwait(false);
        if (Capabilities.HasFlag(Capabilities.Broadcaster) && Broadcaster is BleBroadcaster broadcaster)
            await broadcaster.DisposeAsync().ConfigureAwait(false);
        if (Capabilities.HasFlag(Capabilities.Peripheral) && Peripheral is BlePeripheral peripheral)
            await peripheral.DisposeAsync().ConfigureAwait(false);
    }
}
