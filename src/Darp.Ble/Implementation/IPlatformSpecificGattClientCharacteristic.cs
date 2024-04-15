using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Implementation;


public enum GattProtocolStatus : byte
{
    Success = 0x00,
    WriteRequestRejected = 0xFC,
    ClientCharacteristicConfigurationDescriptorImproperlyConfigured = 0xFD,
    ProcedureAlreadyInProgress = 0xFE,
    OutOfRange = 0xFF,
}

public interface IPlatformSpecificGattClientCharacteristic
{
    GattProperty Property { get; }
    IDisposable OnWrite(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback);
    Task<bool> NotifyAsync(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken);
}