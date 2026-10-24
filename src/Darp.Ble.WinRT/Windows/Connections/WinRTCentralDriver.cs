using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Darp.Ble.WinRT.Connections;

/// <summary>
/// Connects through a GATT session that keeps the connection up. Windows chooses the connection parameters;
/// the connection exists once the session is active
/// </summary>
internal sealed class WinRTCentralDriver : ICentralDriver
{
    public async Task<IConnectionDriver> ConnectAsync(
        BleAddress address,
        BleConnectionParameters parameters,
        IConnectionSink sink,
        CancellationToken cancellationToken
    )
    {
        BluetoothLEDevice? device = await BluetoothLEDevice
            .FromBluetoothAddressAsync(address.Value, WinRTMapping.ToWindows(address.Type))
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (device is null)
            throw new BleConnectException(BleConnectErrorKind.Unreachable, $"Windows does not know a device {address}");
        GattSession? session = null;
        try
        {
            session = await GattSession
                .FromDeviceIdAsync(device.BluetoothDeviceId)
                .AsTask(cancellationToken)
                .ConfigureAwait(false);
            var active = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnStatus(GattSession sender, GattSessionStatusChangedEventArgs args)
            {
                if (args.Status is GattSessionStatus.Active)
                    active.TrySetResult();
            }
            session.SessionStatusChanged += OnStatus;
            try
            {
                session.MaintainConnection = true;
                if (session.SessionStatus is GattSessionStatus.Active)
                    active.TrySetResult();
                // Windows keeps trying; the caller decides how long
                await active.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                session.SessionStatusChanged -= OnStatus;
            }
            return new WinRTConnectionDriver(device, session, sink);
        }
        catch
        {
            if (session is not null)
            {
                session.MaintainConnection = false;
                session.Dispose();
            }
            device.Dispose();
            throw;
        }
    }
}
