using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace Darp.Ble.WinRT;

/// <summary>
/// One GATT request as a Windows call. Windows hides its own queue and the transactions inside a call, so the
/// request counts as sent once the call started, and the ATT timeout includes Windows' queueing. A started call is
/// never cancelled natively: its effect on a sent request is undocumented (D11)
/// </summary>
internal static class NativeCall
{
    public static async Task<T> RunAsync<T>(GattRequest request, Func<IAsyncOperation<T>> start)
    {
        request.HandOffToken.ThrowIfCancellationRequested();
        IAsyncOperation<T> operation = start();
        request.OnTransactionSent();
        try
        {
            return await operation.AsTask().ConfigureAwait(false);
        }
        finally
        {
            request.OnTransactionAnswered();
        }
    }

    /// <summary> Fails with the peer's ATT error or the reason Windows could not reach it </summary>
    public static void ThrowIfFailed(GattCommunicationStatus status, byte? protocolError, string action)
    {
        switch (status)
        {
            case GattCommunicationStatus.Success:
                return;
            case GattCommunicationStatus.ProtocolError:
                throw new GattException(
                    (GattProtocolStatus)(protocolError ?? (byte)GattProtocolStatus.UnlikelyError),
                    $"The peer answered {action} with {protocolError}"
                );
            default:
                throw new BleException($"Windows could not {action}: {status}");
        }
    }
}
