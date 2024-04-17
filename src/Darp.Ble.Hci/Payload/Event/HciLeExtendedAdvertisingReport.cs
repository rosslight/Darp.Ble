using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Event;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciLeExtendedAdvertisingReport : IDecodable<HciLeExtendedAdvertisingReport>
{
    public required ushort EventType { get; init; }
    public required byte AddressType { get; init; }
    public required DeviceAddress Address { get; init; }
    public required byte PrimaryPhy { get; init; }
    public required byte SecondaryPhy { get; init; }
    public required byte AdvertisingSId { get; init; }
    public required sbyte TxPower { get; init; }
    public required sbyte Rssi { get; init; }
    public required ushort PeriodicAdvertisingInterval { get; init; }
    public required byte DirectAddressType { get; init; }
    public required DeviceAddress DirectAddress { get; init; }
    public required byte DataLength { get; init; }
    public required ReadOnlyMemory<byte> Data { get; init; }

    public static bool TryDecode(in ReadOnlyMemory<byte> source,
        out HciLeExtendedAdvertisingReport result,
        out int bytesDecoded)
    {
        result = default;
        bytesDecoded = default;
        ReadOnlySpan<byte> span = source.Span;
        if (!BinaryPrimitives.TryReadUInt16LittleEndian(span, out ushort eventType)) return false;
        if (!IDecodable<HciLeExtendedAdvertisingReport>.TryReadUInt8(span[2..], out byte addressType)) return false;
        if (!DeviceAddress.TryDecode(source[3..], out DeviceAddress address, out int _)) return false;
        if (!IDecodable<HciLeExtendedAdvertisingReport>.TryReadUInt8(span[9..], out byte primaryPhy)) return false;
        if (!IDecodable<HciLeExtendedAdvertisingReport>.TryReadUInt8(span[10..], out byte secondaryPhy)) return false;
        if (!IDecodable<HciLeExtendedAdvertisingReport>.TryReadUInt8(span[11..], out byte advertisingSId)) return false;
        if (!IDecodable<HciLeExtendedAdvertisingReport>.TryReadInt8(span[12..], out sbyte txPower)) return false;
        if (!IDecodable<HciLeExtendedAdvertisingReport>.TryReadInt8(span[13..], out sbyte rssi)) return false;
        if (!BinaryPrimitives.TryReadUInt16LittleEndian(span[14..], out ushort periodicAdvertisingInterval)) return false;
        if (!IDecodable<HciLeExtendedAdvertisingReport>.TryReadUInt8(span[16..], out byte directAddressType)) return false;
        if (!DeviceAddress.TryDecode(source[17..], out DeviceAddress directAddress, out int _)) return false;
        if (!IDecodable<HciLeExtendedAdvertisingReport>.TryReadUInt8(span[23..], out byte dataLength)) return false;
        bytesDecoded = 24 + dataLength;
        result = new HciLeExtendedAdvertisingReport
        {
            EventType = eventType,
            AddressType = addressType,
            Address = address,
            PrimaryPhy = primaryPhy,
            SecondaryPhy = secondaryPhy,
            AdvertisingSId = advertisingSId,
            TxPower = txPower,
            Rssi = rssi,
            PeriodicAdvertisingInterval = periodicAdvertisingInterval,
            DirectAddressType = directAddressType,
            DirectAddress = directAddress,
            DataLength = dataLength,
            Data = source[24..bytesDecoded],
        };
        return true;
    }
}