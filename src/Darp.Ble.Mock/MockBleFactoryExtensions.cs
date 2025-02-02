namespace Darp.Ble.Mock;

/// <summary> Extension methods for easier addition of a <see cref="BleMockFactory"/> </summary>
public static class MockBleFactoryExtensions
{
    /// <summary> Add a new <see cref="BleMockFactory"/> and configure it. </summary>
    /// <param name="builder"> An optional callback to configure the factory </param>
    /// <param name="configure"> The callback to configure the factory </param>
    /// <returns> The <paramref name="builder"/> </returns>
    public static BleManagerBuilder AddMock(this BleManagerBuilder builder, Action<BleMockFactory>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.Add(configure);
    }
}