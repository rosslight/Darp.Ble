using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gap;
using Darp.Ble.Utils;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Implementation;

/// <summary> The ble observer </summary>
/// <param name="device"> The ble device </param>
/// <param name="logger"> The logger </param>
public abstract class BleObserver(BleDevice device, ILogger<BleObserver> logger) : IBleObserver
{
    private readonly BleDevice _bleDevice = device;
    private readonly SemaphoreSlim _observationSemaphore = new(1, 1);

    /// <summary> Usage should always be inside a wait-statement of <see cref="_observationSemaphore"/> </summary>
    private readonly List<(Action<IGapAdvertisement> OnAdvertisement, Action OnStopped)> _actions = [];
    private IDisposable? _observationDisposable;

    /// <summary> The logger </summary>
    protected ILogger<BleObserver> Logger { get; } = logger;

    /// <summary> The service provider </summary>
    protected IServiceProvider ServiceProvider => Device.ServiceProvider;

    /// <inheritdoc />
    public IBleDevice Device => _bleDevice;

    /// <inheritdoc />
    public BleScanParameters Parameters { get; private set; } =
        new()
        {
            ScanType = ScanType.Passive,
            ScanInterval = ScanTiming.Ms100,
            ScanWindow = ScanTiming.Ms100,
        };

    /// <inheritdoc />
    public bool IsScanning => _observationDisposable is not null;

    /// <inheritdoc />
    public bool Configure(BleScanParameters parameters)
    {
        if (IsScanning)
            return false;
        Parameters = parameters;
        return true;
    }

    /// <inheritdoc />
    public async Task<IAsyncDisposable> StartObservingAsync(
        Action<IGapAdvertisement> onAdvertisement,
        Action onStopped,
        CancellationToken cancellationToken
    )
    {
        ObjectDisposedException.ThrowIf(_bleDevice.IsDisposing, nameof(BleObserver));
        await _observationSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        (Action<IGapAdvertisement> onAdvertisement, Action onStopped) action = (onAdvertisement, onStopped);
        try
        {
            if (_observationDisposable is null)
            {
                try
                {
                    _observationDisposable = await StartObservingAsyncCore(
                            this,
                            static (self, advertisement) =>
                            {
                                // Reversed for loop. Actions might be removed from the list on Invoke
                                for (int index = self._actions.Count - 1; index >= 0; index--)
                                {
                                    if (self._actions.Count is 0)
                                        return;
                                    Action<IGapAdvertisement> onAdvertisement = self._actions[index].OnAdvertisement;
                                    onAdvertisement(advertisement);
                                }
                            },
                            cancellationToken
                        )
                        .ConfigureAwait(false);
                }
                catch (Exception e) when (e is not BleObservationStartException)
                {
                    throw new BleObservationStartException(this, e.Message, e);
                }

                Logger.LogTrace("Started observation");
            }
            _actions.Add(action);
        }
        finally
        {
            _observationSemaphore.Release();
        }
        return AsyncDisposable.Create(
            (Self: this, Action: action),
            static async state =>
            {
                BleObserver self = state.Self;
                await self._observationSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                try
                {
                    self._actions.Remove(state.Action);
                    if (self._actions.Count > 0)
                        return;
                    await self.StopObservingUnsafeAsync().ConfigureAwait(false);
                }
                finally
                {
                    self._observationSemaphore.Release();
                }
            }
        );
    }

    protected abstract Task<IDisposable> StartObservingAsyncCore<TState>(
        TState state,
        Action<TState, IGapAdvertisement> onAdvertisement,
        CancellationToken cancellationToken
    );

    /// <inheritdoc />
    public async Task StopObservingAsync()
    {
        try
        {
            await _observationSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            await StopObservingUnsafeAsync().ConfigureAwait(false);
        }
        finally
        {
            _observationSemaphore.Release();
        }
    }

    /// <summary> Only intended to be called when synchronized using <see cref="_observationSemaphore"/> </summary>
    private async Task StopObservingUnsafeAsync()
    {
        for (int index = _actions.Count - 1; index >= 0; index--)
        {
            Action onStopped = _actions[index].OnStopped;
            onStopped();
        }
        _actions.Clear();
        await StopObservingAsyncCore().ConfigureAwait(false);
        Logger.LogTrace("Stopped advertising observation");
        _observationDisposable?.Dispose();
        _observationDisposable = null;
    }

    /// <summary> Core implementation of stopping </summary>
    protected abstract Task StopObservingAsyncCore();

    /// <summary> A method that can be used to clean up all resources. </summary>
    /// <remarks> This method is not glued to the <see cref="IAsyncDisposable"/> interface. All disposes should be done using the  </remarks>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
    }

    /// <inheritdoc cref="DisposeAsync"/>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

    /// <inheritdoc cref="IDisposable.Dispose"/>
    /// <param name="disposing">
    /// True, when this method was called by the synchronous <see cref="IDisposable.Dispose"/> method;
    /// False if called by the asynchronous <see cref="IAsyncDisposable.DisposeAsync"/> method
    /// </param>
    protected virtual void Dispose(bool disposing) { }
}
