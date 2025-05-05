using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Client;
using static Darp.Ble.Gatt.CharacteristicDeclaration;
using static Darp.Ble.Gatt.Properties;

namespace Darp.Ble.Tests.Gatt.Characteristic;

#pragma warning disable MA0004 // Use Task. ConfigureAwait(false) if the current SynchronizationContext is not needed

public sealed class Test
{
    public static BleUuid SomeUuid = 0x1234;
    public static byte[] SomeBytes = [];
    public static IObservable<byte[]> SomeByteObservable = Observable.Return(Convert.FromHexString(""));
    public static IObservable<int> SomeIntObservable = Observable.Return(1);
    public static CharacteristicDeclaration<Read> Char1 { get; } = new(SomeUuid);
    public static TypedCharacteristicDeclaration<int, Read> ReadChar { get; } = Create<int, Read>(SomeUuid);
    public static TypedCharacteristicDeclaration<int, Write> WriteChar { get; } = Create<int, Write>(SomeUuid);
    public static TypedCharacteristicDeclaration<int, Notify> NotifyChar { get; } = Create<int, Notify>(SomeUuid);
    public static TypedCharacteristicDeclaration<int, Read, Notify> ReadNotifyChar { get; } =
        Create<int, Read, Notify>(SomeUuid);
    public static TypedCharacteristicDeclaration<int, Read, Write> ReadWriteChar { get; } =
        Create<int, Read, Write>(SomeUuid);
    public static CharacteristicDeclaration<Write> Char2 { get; } = new(SomeUuid);
    public static CharacteristicDeclaration<Notify> Char3 { get; } = new(SomeUuid);
    public static CharacteristicDeclaration<Indicate> Char4 { get; } = new(SomeUuid);
    public static CharacteristicDeclaration<Read, Write> Char5 { get; } = new(SomeUuid);

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
        var x = service.AddCharacteristic<Read>(SomeUuid, SomeBytes);
        service.AddCharacteristic<Read>(SomeUuid, _ => SomeBytes);
        service.AddCharacteristic(ReadChar);
        var aa = service.AddCharacteristic(ReadChar, 2);
        await aa.UpdateValueAsync(123);
        service.AddCharacteristic(ReadChar, _ => 2);

        service.AddCharacteristic<Write>(SomeUuid, onWrite: (peer, bytes) => GattProtocolStatus.Success);
        service.AddCharacteristic<Write>(
            SomeUuid,
            onWrite: (peer, bytes) => ValueTask.FromResult(GattProtocolStatus.Success)
        );
        service.AddCharacteristic(WriteChar);
        service.AddCharacteristic(WriteChar, onWrite: (peer, number) => GattProtocolStatus.Success);

        IGattClientPeer? peer = null;
        var cn1 = service.AddCharacteristic<Notify>(SomeUuid);
        var cn11 = service.AddCharacteristic<Notify, Read>(SomeUuid, SomeBytes);
        await cn1.NotifyAllAsync(SomeBytes);
        await cn11.UpdateValueAsync(SomeBytes);
        await cn11.NotifyAsync(peer, SomeBytes);
        await cn11.NotifyAllAsync(SomeBytes);

        var cn2 = service.AddCharacteristic(NotifyChar);
        await cn2.NotifyAllAsync(3);
        var cn3 = service.AddCharacteristic(ReadNotifyChar, 3);
        //cn3.UpdateValue(SomeBytes);
        await cn3.UpdateValueAsync(32);
        var cn4 = service.AddCharacteristic<int, Write, Read>(ReadWriteChar, 123123);
        await cn4.UpdateValueAsync(3);
        int value = await cn4.GetValueAsync<int>();
    }

    public void XX()
    {
        IGattClientService service = null!;
        var readChar = service.AddCharacteristic<Read>(SomeUuid, onRead: _ => SomeBytes);
        var writeChar = service.AddCharacteristic<Write>(
            SomeUuid,
            onWrite: async (_, bytes) =>
            {
                await readChar.UpdateValueAsync(bytes);
                return GattProtocolStatus.Success;
            }
        );
    }
}
