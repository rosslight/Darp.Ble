namespace Darp.Ble.Mock;

/// <summary> Shortcuts for tests that need an adapter without setting up a <see cref="BleManager"/> </summary>
public static class MockBle
{
    /// <summary> Opens a mock adapter. Public types of the library are sealed, so tests use a real mock adapter instead of a substitute </summary>
    /// <param name="name"> The name of the adapter </param>
    /// <param name="cancellationToken"> Cancels opening </param>
    /// <returns> The open mock adapter; the caller owns it </returns>
    public static async Task<BleAdapter> OpenAdapterAsync(
        string name = "Mock",
        CancellationToken cancellationToken = default
    )
    {
        var manager = new BleManager(builder => builder.AddMock(options => options.Name = name));
        IReadOnlyList<BleAdapterInfo> adapters = await manager
            .EnumerateAdaptersAsync(cancellationToken)
            .ConfigureAwait(false);
        return await adapters[0].OpenAsync(cancellationToken).ConfigureAwait(false);
    }
}
