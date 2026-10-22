namespace Darp.Ble.Backends;

/// <summary> A backend that connects the library to a platform or a radio, e.g. the mock, HCI or WinRT backend </summary>
/// <remarks> Implemented only by the backends of this library; apps register backends through their builder extensions </remarks>
public interface IBleBackend
{
    /// <summary> The name apps see as <see cref="BleAdapterInfo.Backend"/>, e.g. <see cref="BleBackends.Mock"/> </summary>
    string Name { get; }

    /// <summary> Lists the adapters this backend can open right now </summary>
    /// <param name="context"> The services of the <see cref="BleManager"/> that asks </param>
    /// <param name="cancellationToken"> Cancels the enumeration </param>
    /// <returns> One source per adapter, in the order the backend prefers </returns>
    Task<IReadOnlyList<IBleAdapterSource>> EnumerateAdaptersAsync(
        BleBackendContext context,
        CancellationToken cancellationToken
    );
}
