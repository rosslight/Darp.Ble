using Darp.Ble.Backends;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.WinRT;

/// <summary> Windows' Bluetooth stack: one adapter, the system's default </summary>
internal sealed class WinRTBackend : IBleBackend
{
    public string Name => BleBackends.WinRT;

    public Task<IReadOnlyList<IBleAdapterSource>> EnumerateAdaptersAsync(
        BleBackendContext context,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
#if WINDOWS
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            return Task.FromResult<IReadOnlyList<IBleAdapterSource>>([new WinRTAdapterSource()]);
#endif
        context.LoggerFactory.CreateLogger<WinRTBackend>().LogNotOnWindows();
        return Task.FromResult<IReadOnlyList<IBleAdapterSource>>([]);
    }
}
