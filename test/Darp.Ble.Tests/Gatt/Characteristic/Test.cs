using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using static Darp.Ble.Gatt.CharacteristicDeclaration;

namespace Darp.Ble.Tests.Gatt.Characteristic;

#pragma warning disable MA0004 // Use Task. ConfigureAwait(false) if the current SynchronizationContext is not needed

public sealed class Test
{
    public static BleUuid SomeUuid = 0x1234;
    public static byte[] SomeBytes = [];
    public static IObservable<byte[]> SomeByteObservable = Observable.Return(Convert.FromHexString(""));
    public static IObservable<int> SomeIntObservable = Observable.Return(1);
    public static CharacteristicDeclaration<Properties.Read> Char1 { get; } = new(SomeUuid);
    public static TypedCharacteristicDeclaration<int, Properties.Read> ReadChar { get; } = Create<int, Properties.Read>(SomeUuid);
    public static TypedCharacteristicDeclaration<int, Properties.Write> WriteChar { get; } = Create<int, Properties.Write>(SomeUuid);
    public static TypedCharacteristicDeclaration<int, Properties.Notify> NotifyChar { get; } = Create<int, Properties.Notify>(SomeUuid);
    public static TypedCharacteristicDeclaration<int, Properties.Read, Properties.Notify> ReadNotifyChar { get; } = Create<int, Properties.Read, Properties.Notify>(SomeUuid);
    public static TypedCharacteristicDeclaration<int, Properties.Read, Properties.Write> ReadWriteChar { get; } = Create<int, Properties.Read, Properties.Write>(SomeUuid);
    public static CharacteristicDeclaration<Properties.Write> Char2 { get; } = new(SomeUuid);
    public static CharacteristicDeclaration<Properties.Notify> Char3 { get; } = new(SomeUuid);
    public static CharacteristicDeclaration<Properties.Indicate> Char4 { get; } = new(SomeUuid);
    public static CharacteristicDeclaration<Properties.Read, Properties.Write> Char5 { get; } = new(SomeUuid);
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
        await service.AddCharacteristicAsync(ReadChar);
        await service.AddCharacteristicAsync(ReadChar, 2);
        await service.AddCharacteristicAsync(ReadChar, _ => 2);

        await service.AddCharacteristicAsync<Properties.Write>(SomeUuid, onWrite: (peer, bytes) => GattProtocolStatus.Success);
        await service.AddCharacteristicAsync<Properties.Write>(SomeUuid, onWrite: (peer, bytes, token) => ValueTask.FromResult(GattProtocolStatus.Success));
        await service.AddCharacteristicAsync(WriteChar);
        await service.AddCharacteristicAsync(WriteChar, onWrite: (peer, number) => GattProtocolStatus.Success);

        IGattClientPeer? peer = null;
        var cn1 = await service.AddCharacteristicAsync<Properties.Notify>(SomeUuid);
        var cn11 = await service.AddCharacteristicAsync<Properties.Notify, Properties.Read>(SomeUuid, SomeBytes);
        cn1.NotifyAll(SomeBytes);
        cn11.UpdateValue(SomeBytes);
        cn11.Notify(peer, SomeBytes);
        cn11.NotifyAll(SomeBytes);

        var cn2 = await service.AddCharacteristicAsync(NotifyChar);
        cn2.NotifyAll(3);
        var cn3 = await service.AddCharacteristicAsync(ReadNotifyChar, 3);
        cn3.UpdateValue(SomeBytes);
        cn3.UpdateValue(32);
        var cn4 = await service.AddCharacteristicAsync<int, Properties.Write, Properties.Read>(ReadWriteChar, 123123);
        cn4.UpdateValue(3);
        int value = cn4.GetValue<int>();
    }
}