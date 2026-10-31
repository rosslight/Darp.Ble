namespace Darp.Ble.HciHost.Gatt;

/// <summary> A request waiting for its answer </summary>
internal interface IPendingResponse
{
    void Fail(Exception exception);
}
