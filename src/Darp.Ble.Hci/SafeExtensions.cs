using Darp.Ble.Hci.Payload;

namespace Darp.Ble.Hci;

public static class SafeExtensions
{
    [Obsolete("Having parameters with event type is not ok", true)]
    public static ValueTask<TParameters> QueryCommandAsync<TCommand, TParameters>(this HciHost hciHost,
        TCommand command = default)
        where TCommand : unmanaged, IHciCommand<TCommand>
        where TParameters : unmanaged, IHciEvent<TParameters>
    {
        throw new NotSupportedException();
    }
}