using Darp.Ble.Backends;
using Windows.Foundation;

namespace Darp.Ble.WinRT;

/// <summary>
/// One GATT request as a Windows call. Windows hides its own queue and the transactions inside a call, so the
/// request counts as sent once the call started, and the ATT timeout includes Windows' queueing. A started call is
/// never cancelled natively: its effect on a sent request is undocumented
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
}
