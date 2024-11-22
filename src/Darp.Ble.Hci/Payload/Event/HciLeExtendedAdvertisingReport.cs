using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Event;

/// <summary> All parameters of the <see cref="HciLeExtendedAdvertisingReportEvent"/> </summary>
/// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host-controller-interface/host-controller-interface-functional-specification.html#UUID-e88970dc-edc8-ca27-58d8-153b97751686"/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HciLeExtendedAdvertisingReport : IDecodable<HciLeExtendedAdvertisingReport>
{
    /// <summary> The Event_Type </summary>
    public required ushort EventType { get; init; }
    /// <summary> The Address_Type </summary>
    public required byte AddressType { get; init; }
    /// <summary> The Address </summary>
    public required DeviceAddress Address { get; init; }
    /// <summary> The Primary_PHY </summary>
    public required byte PrimaryPhy { get; init; }
    /// <summary> The Secondary_PHY </summary>
    public required byte SecondaryPhy { get; init; }
    /// <summary> The Advertising_SID </summary>
    public required byte AdvertisingSId { get; init; }
    /// <summary> The TX_Power </summary>
    public required sbyte TxPower { get; init; }
    /// <summary> The RSSI </summary>
    public required sbyte Rssi { get; init; }
    /// <summary> The Periodic_Advertising_Interval </summary>
    public required ushort PeriodicAdvertisingInterval { get; init; }
    /// <summary> The Direct_Address_Type </summary>
    public required byte DirectAddressType { get; init; }
    /// <summary> The Direct_Address </summary>
    public required DeviceAddress DirectAddress { get; init; }
    /// <summary> The Data_Length </summary>
    public required byte DataLength { get; init; }
    /// <summary> The Data </summary>
    public required ReadOnlyMemory<byte> Data { get; init; }

    /// <inheritdoc />
    public static bool TryDecode(in ReadOnlyMemory<byte> source,
        out HciLeExtendedAdvertisingReport result,
        out int bytesDecoded)
    {
        result = default;
        bytesDecoded = default;
        ReadOnlySpan<byte> span = source.Span;
        if (!BinaryPrimitives.TryReadUInt16LittleEndian(span, out ushort eventType)) return false;
        if (!TryReadUInt8(span[2..], out byte addressType)) return false;
        if (!DeviceAddress.TryDecode(source[3..], out DeviceAddress address, out int _)) return false;
        if (!TryReadUInt8(span[9..], out byte primaryPhy)) return false;
        if (!TryReadUInt8(span[10..], out byte secondaryPhy)) return false;
        if (!TryReadUInt8(span[11..], out byte advertisingSId)) return false;
        if (!TryReadInt8(span[12..], out sbyte txPower)) return false;
        if (!TryReadInt8(span[13..], out sbyte rssi)) return false;
        if (!BinaryPrimitives.TryReadUInt16LittleEndian(span[14..], out ushort periodicAdvertisingInterval)) return false;
        if (!TryReadUInt8(span[16..], out byte directAddressType)) return false;
        if (!DeviceAddress.TryDecode(source[17..], out DeviceAddress directAddress, out int _)) return false;
        if (!TryReadUInt8(span[23..], out byte dataLength)) return false;
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

    private static bool TryReadUInt8(ReadOnlySpan<byte> source, out byte value)
    {
        if (source.Length == 0)
        {
            value = default;
            return false;
        }
        value = source[0];
        return true;
    }
    private static bool TryReadInt8(ReadOnlySpan<byte> source, out sbyte value)
    {
        if (source.Length == 0)
        {
            value = default;
            return false;
        }
        value = (sbyte)source[0];
        return true;
    }
}