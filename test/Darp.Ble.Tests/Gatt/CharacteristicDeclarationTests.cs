using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Shouldly;

namespace Darp.Ble.Tests.Gatt;

/// <summary> Declarations of unmanaged values, which go on the wire as a copy of the value's memory </summary>
public sealed class CharacteristicDeclarationTests
{
    private static readonly BleUuid Uuid = BleUuid.FromUInt16(0xFFF1);

    [Fact]
    public void AnUnmanagedValue_GoesOnTheWireWithItsInMemorySize()
    {
        IGattTypedCharacteristic<bool> flag = CharacteristicDeclaration.Create<bool, Properties.Read>(Uuid);
        IGattTypedCharacteristic<char> letter = CharacteristicDeclaration.Create<char, Properties.Read>(Uuid);
        IGattTypedCharacteristic<DateTime> time = CharacteristicDeclaration.Create<DateTime, Properties.Read>(Uuid);
        var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

        flag.Encode(true).ShouldBe([1]);
        flag.Decode([1]).ShouldBeTrue();
        letter.Encode('A').ShouldBe([0x41, 0x00]);
        letter.Decode([0x41, 0x00]).ShouldBe('A');
        time.Decode(time.Encode(now)).ShouldBe(now);
    }
}
