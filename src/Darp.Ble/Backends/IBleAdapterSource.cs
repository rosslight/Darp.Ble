using System.Diagnostics.CodeAnalysis;

namespace Darp.Ble.Backends;

/// <summary> One adapter a backend enumerated; the core shows it to apps as <see cref="BleAdapterInfo"/> </summary>
[Experimental(Experiments.Backends)]
public interface IBleAdapterSource
{
    /// <summary> The name of the adapter, if the backend knows one </summary>
    string? Name { get; }

    /// <summary> Opens the adapter and returns its driver once it is ready to use </summary>
    /// <param name="context"> The services of the <see cref="BleManager"/> that opens the adapter </param>
    /// <param name="cancellationToken"> Cancels opening </param>
    /// <returns> The driver of the opened adapter; the core owns it from here on </returns>
    /// <exception cref="BleOpenException"> The adapter could not be opened </exception>
    Task<IBleAdapterDriver> OpenAsync(BleBackendContext context, CancellationToken cancellationToken);
}
