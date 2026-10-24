using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Transport;

namespace Darp.Ble.HciHost;

/// <summary> One HCI controller; opening it creates its transport and sets the controller up </summary>
internal sealed class HciHostAdapterSource(
    string? name,
    Func<BleBackendContext, ITransportLayer> createTransport,
    BleAddress? randomAddress,
    HciSettings settings
) : IBleAdapterSource
{
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
            return await HciHostAdapterDriver.OpenAsync(device, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await device.DisposeAsync().ConfigureAwait(false);
            if (e is OperationCanceledException && cancellationToken.IsCancellationRequested)
                throw;
            throw new BleOpenException(
                BleOpenErrorKind.NotAvailable,
                $"The HCI controller '{Name}' could not be set up: {e.Message}",
                isTransient: e is TimeoutException or IOException or UnauthorizedAccessException,
                innerException: e
            );
        }
    }
}
