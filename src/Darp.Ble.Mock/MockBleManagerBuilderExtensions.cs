namespace Darp.Ble.Mock;

/// <summary> Registers the mock backend </summary>
public static class MockBleManagerBuilderExtensions
{
    /// <summary> Adds the in-process mock backend. It lists one mock adapter that scans and advertises, plus the simulated peripherals it sees </summary>
    /// <param name="builder"> The builder of the <see cref="BleManager"/> </param>
    /// <param name="configure"> Configures the mock backend </param>
    /// <returns> The builder </returns>
    public static BleManagerBuilder AddMock(
        this BleManagerBuilder builder,
        Action<MockBackendOptions>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        var options = new MockBackendOptions();
        configure?.Invoke(options);
        return builder.AddBackend(
            new MockBackend(
                new MockSettings(options.Name, options.OpenFailure, [.. options.Peripherals], options.TxPowerToRssi)
            )
        );
    }
}
