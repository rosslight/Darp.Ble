using Darp.Ble.Backends;
using Darp.Ble.Diagnostics;

namespace Darp.Ble;

/// <summary> Collects the backends of a <see cref="BleManager"/> </summary>
public sealed class BleManagerBuilder
{
    private readonly List<IBleBackend> _backends = [];
    private readonly BleQueueDefaults _queues = new();
    private readonly BleDiagnosticsOptions _diagnostics = new();

    internal BleManagerBuilder() { }

    /// <summary> Adds a backend. Backend packages call this from their extensions, e.g. <c>AddMock()</c> </summary>
    /// <param name="backend"> The backend to add; adapters are enumerated in the order backends were added </param>
    /// <returns> This builder </returns>
    public BleManagerBuilder AddBackend(IBleBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);
        _backends.Add(backend);
        return this;
    }

    /// <summary> Changes the default queues of streams, e.g. how many advertisements a scan buffers </summary>
    /// <param name="configure"> Changes the defaults </param>
    /// <returns> This builder </returns>
    public BleManagerBuilder ConfigureQueues(Action<BleQueueDefaults> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_queues);
        return this;
    }

    /// <summary> Changes what the library's logs and spans may contain </summary>
    /// <param name="configure"> Changes the options </param>
    /// <returns> This builder </returns>
    public BleManagerBuilder ConfigureDiagnostics(Action<BleDiagnosticsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_diagnostics);
        return this;
    }

    internal (IBleBackend[] Backends, BleCoreOptions Options) Build() =>
        (
            [.. _backends],
            new BleCoreOptions(
                _queues.Advertisements,
                _queues.Notifications,
                // A snapshot: later changes to the builder's options do not reach a manager already built
                new DiagnosticsSettings(_diagnostics.IncludeAddresses)
            )
        );
}
