using Darp.Ble.Backends;
using Darp.Ble.Mock.Connections;

namespace Darp.Ble.Mock;

/// <summary> The mock adapter. Opening it creates its air and opens its simulated peripherals </summary>
internal sealed class MockAdapterSource(MockSettings settings) : IBleAdapterSource
{
    private readonly MockSettings _settings = settings;

    public string Name => _settings.Name;

    public async Task<IBleAdapterDriver> OpenAsync(BleBackendContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_settings.OpenFailure is { } kind)
            throw new BleOpenException(kind, $"The mock adapter '{Name}' is configured to fail opening with {kind}");

        var air = new MockAir(context.TimeProvider, _settings.TxPowerToRssi);
        var driver = new MockAdapterDriver(air, _settings.IncludeGattService);
        try
        {
            foreach (MockPeripheral peripheral in _settings.Peripherals)
            {
                BleAdapter adapter = await OpenPeripheralAsync(air, peripheral, context, cancellationToken)
                    .ConfigureAwait(false);
                // The mock adapter owns its peripherals from here on and disposes them with itself
                driver.AddPeripheral(adapter);
                await peripheral.Configure(adapter).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
            return driver;
        }
        catch
        {
            await driver.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary> Opens a peripheral through the public path, so its callback gets a real <see cref="BleAdapter"/> </summary>
    private static async Task<BleAdapter> OpenPeripheralAsync(
        MockAir air,
        MockPeripheral peripheral,
        BleBackendContext context,
        CancellationToken cancellationToken
    )
    {
        var manager = new BleManager(
            // The simulated peripherals follow the mock adapter's diagnostics options
            builder =>
                builder
                    .AddBackend(new MockPeripheralBackend(air, peripheral))
                    .ConfigureDiagnostics(options => options.IncludeAddresses = context.IncludeAddresses),
            context.LoggerFactory,
            context.TimeProvider
        );
        IReadOnlyList<BleAdapterInfo> adapters = await manager
            .EnumerateAdaptersAsync(cancellationToken)
            .ConfigureAwait(false);
        return await adapters[0].OpenAsync(cancellationToken).ConfigureAwait(false);
    }
}
