using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Transport;
using Microsoft.Win32.SafeHandles;

namespace Darp.Ble.HciHost;

/// <summary> One HCI controller; opening it creates its transport and sets the controller up </summary>
internal sealed class HciHostAdapterSource(
    string? name,
    string? portName,
    Func<BleBackendContext, ITransportLayer> createTransport,
    BleAddress? randomAddress,
    HciSettings settings,
    bool includeGattService
) : IBleAdapterSource
{
    // The serial port of the transport; null for a transport of the app's own
    private readonly string? _portName = portName;
    private readonly bool _includeGattService = includeGattService;
    private readonly Func<BleBackendContext, ITransportLayer> _createTransport = createTransport;
    private readonly BleAddress? _randomAddress = randomAddress;
    private readonly HciSettings _settings = settings;

    public string? Name { get; } = name;

    public async Task<IBleAdapterDriver> OpenAsync(BleBackendContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BleAddress address = _randomAddress ?? BleAddress.NewRandomStaticAddress();
#pragma warning disable CA2000 // The driver owns the device; a failed setup disposes it below
        var device = new HciDevice(
            _createTransport(context),
            address.Value,
            _settings,
            context.TimeProvider,
            context.LoggerFactory
        );
#pragma warning restore CA2000
        try
        {
            return await HciHostAdapterDriver
                .OpenAsync(device, _includeGattService, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await device.DisposeAsync().ConfigureAwait(false);
            if (e is OperationCanceledException && cancellationToken.IsCancellationRequested)
                throw;
            if (e is UnauthorizedAccessException && FindDenial() is { } denial)
            {
                throw new BleOpenException(
                    BleOpenErrorKind.MissingPermission,
                    $"The user may not open the serial port '{_portName}' of the HCI controller '{Name}'; on Linux, "
                        + "the user needs to be in the group that owns the port, e.g. dialout",
                    isTransient: false,
                    nativeCode: denial.InnerException?.HResult,
                    innerException: denial
                );
            }
            // Windows opens a serial port exclusively and denies it while another process uses it; its error does not
            // tell that apart from an access rule, the rarer cause
            throw new BleOpenException(
                BleOpenErrorKind.NotAvailable,
                $"The HCI controller '{Name}' could not be set up: {e.Message}",
                isTransient: e is TimeoutException or IOException or UnauthorizedAccessException,
                innerException: e
            );
        }
    }

    /// <summary>
    /// Why the serial port could not be opened, when the user may not open it. Outside Windows every failure to open a
    /// serial port is an <see cref="UnauthorizedAccessException"/>, also for a port that is not there, and on Linux the
    /// error number in it is wrong on the first open of a process. Opening the port as a file tells the cause; it did not
    /// open just now, so this does not touch the device
    /// </summary>
    /// <returns> The denial, with the platform's error number in its inner exception; null when the cause is another </returns>
    private UnauthorizedAccessException? FindDenial()
    {
        if (_portName is null || OperatingSystem.IsWindows())
            return null;
        try
        {
            using SafeFileHandle handle = File.OpenHandle(
                _portName,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.ReadWrite
            );
            return null;
        }
        catch (UnauthorizedAccessException denial)
        {
            return denial;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
