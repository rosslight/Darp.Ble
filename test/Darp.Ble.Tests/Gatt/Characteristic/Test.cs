using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Gatt.Services;

namespace Darp.Ble.Tests.Gatt.Characteristic;

#pragma warning disable MA0004 // Use Task. ConfigureAwait(false) if the current SynchronizationContext is not needed

public sealed class Test
{
    public static BleUuid SomeUuid = new BleUuid(0x1234);
    public static byte[] SomeBytes = Convert.FromHexString("");
    public static IObservable<byte[]> SomeByteObservable = Observable.Return(Convert.FromHexString(""));
    public static IObservable<int> SomeIntObservable = Observable.Return(1);
    public static Characteristic<Properties.Read> Char1 { get; } = new(SomeUuid);
    public static Characteristic<Properties.Write> Char2 { get; } = new(SomeUuid);
    public static Characteristic<Properties.Notify> Char3 { get; } = new(SomeUuid);
    public static Characteristic<Properties.Indicate> Char4 { get; } = new(SomeUuid);
    public static Characteristic<Properties.Read, Properties.Write> Char5 { get; } = new(SomeUuid);
    /*
    public static Characteristic<int, Properties.Read> Char11 { get; } = new(SomeUuid);
    public static Characteristic<int, Properties.Write> Char12 { get; } = new(SomeUuid);
    public static Characteristic<int, Properties.Notify> Char13 { get; } = new(SomeUuid);
    public static Characteristic<int, Properties.Indicate> Char14 { get; } = new(SomeUuid);
    public static Characteristic<int, Properties.Read, Properties.Write> Char15 { get; } = new(SomeUuid);
*/
    public async Task X()
    {
        IGattClientService service = null!;
        await service.AddCharacteristicAsync<Properties.Read>(SomeUuid, SomeBytes);
        await service.AddCharacteristicAsync<Properties.Read>(SomeUuid, onRead: _ => SomeBytes);
        await service.AddCharacteristicAsync<int, Properties.Read>(SomeUuid);
        await service.AddCharacteristicAsync<int, Properties.Read>(SomeUuid, 2);
        await service.AddCharacteristicAsync<int, Properties.Read>(SomeUuid, _ => 2);

        await service.AddCharacteristicAsync<Properties.Write>(SomeUuid, onWrite: (peer, bytes) => GattProtocolStatus.Success);
        await service.AddCharacteristicAsync<Properties.Write>(SomeUuid, onWrite: (peer, bytes, token) => ValueTask.FromResult(GattProtocolStatus.Success));
        await service.AddCharacteristicAsync<int, Properties.Write>(SomeUuid);
        await service.AddCharacteristicAsync<int, Properties.Write>(SomeUuid, onWrite: (peer, number) => GattProtocolStatus.Success);

        IGattClientPeer peer = null!;
        var cn1 = await service.AddCharacteristicAsync<Properties.Notify>(SomeUuid);
        var cn11 = await service.AddCharacteristicAsync<Properties.Read, Properties.Notify>(SomeUuid, SomeBytes);
        cn1.NotifyAll(SomeBytes);
        cn11.UpdateValue(SomeBytes);
        cn11.Notify(peer, SomeBytes);
        cn11.NotifyAll(SomeBytes);

        var cn2 = await service.AddCharacteristicAsync<int, Properties.Notify>(SomeUuid);
        cn2.NotifyAll(3);
        var cn3 = await service.AddCharacteristicAsync<int, Properties.Read, Properties.Notify>(SomeUuid, 3);
        cn3.UpdateValue(SomeBytes);
        cn3.UpdateValue(32);
        await service.AddCharacteristicAsync<Properties.Read, Properties.Write>(SomeUuid, SomeBytes);
    }
}