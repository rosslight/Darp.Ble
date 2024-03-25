using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Darp.Ble.Hci.Payload.Command;

namespace Darp.Ble.Hci.Payload.Event;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum HciLeMetaSubEventType : byte
{
    HCI_LE_Data_Length_Change = 0x07,
    /// <summary> BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 4, Part E 7.7.65.13 </summary>
    HCI_LE_Extended_Advertising_Report = 0x0D,
    HCI_LE_Enhanced_Connection_Complete_V1 = 0x0A,
    HCI_LE_Enhanced_Connection_Complete_v2 = 0x29,
}

public interface IHciLeMetaEvent<TEvent> : IHciEvent<TEvent> where TEvent : IHciEvent<TEvent>
{
    static HciEventCode IHciEvent<TEvent>.EventCode => HciEventCode.HCI_LE_Meta;
    static abstract HciLeMetaSubEventType SubEventType { get; }
    HciLeMetaSubEventType SubEventCode { get; }
}

public interface IDecodable<TSelf> where TSelf : IDecodable<TSelf>
{
    static abstract bool TryDecode(in ReadOnlyMemory<byte> source,
        [NotNullWhen(true)] out TSelf? result,
        out int bytesDecoded);
    public static bool TryReadUInt8(ReadOnlySpan<byte> source, out byte value)
    {
        if (source.Length == 0)
        {
            value = default;
            return false;
        }
        value = source[0];
        return true;
    }
    public static bool TryReadInt8(ReadOnlySpan<byte> source, out sbyte value)
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

public interface IDefaultDecodable<TSelf> : IDecodable<TSelf>
    where TSelf : unmanaged, IDefaultDecodable<TSelf>
{
    static bool IDecodable<TSelf>.TryDecode(in ReadOnlyMemory<byte> source,
        out TSelf result,
        out int bytesRead)
    {
        bytesRead = Marshal.SizeOf<TSelf>();
        if (source.Length < bytesRead)
        {
            result = default;
            return false;
        }
        result = source.ToStructUnsafe<TSelf>();
        return true;
    }
}


public static class Extensions
{
    public static T ToStructUnsafe<T>(this in ReadOnlyMemory<byte> memory) where T : unmanaged
    {
        using MemoryHandle memoryHandle = memory.Pin();
        unsafe
        {
            return Unsafe.Read<T>(memoryHandle.Pointer);
        }
    }
}

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
            Data = source[24..bytesDecoded]
        };
        return true;
    }
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct HciLeExtendedAdvertisingReportEvent : IHciLeMetaEvent<HciLeExtendedAdvertisingReportEvent>
{
    public static HciLeMetaSubEventType SubEventType => HciLeMetaSubEventType.HCI_LE_Extended_Advertising_Report;

    public required HciLeMetaSubEventType SubEventCode { get; init; }
    public required byte NumReports { get; init; }
    public required IReadOnlyList<HciLeExtendedAdvertisingReport> Reports { get; init; }

    public static bool TryDecode(in ReadOnlyMemory<byte> buffer,
        out HciLeExtendedAdvertisingReportEvent hciEvent,
        out int bytesRead)
    {
        bytesRead = default;
        hciEvent = default;
        ReadOnlySpan<byte> span = buffer.Span;
        byte subEventCode = span[0];
        byte numReports = span[1];
        var reports = new HciLeExtendedAdvertisingReport[numReports];
        bytesRead = 2;
        for (var i = 0; i < numReports; i++)
        {
            if (!HciLeExtendedAdvertisingReport.TryDecode(buffer[bytesRead..],
                    out HciLeExtendedAdvertisingReport data,
                    out int dataBytesRead))
                return false;
            bytesRead += dataBytesRead;
            reports[i] = data;
        }
        hciEvent = new HciLeExtendedAdvertisingReportEvent
        {
            SubEventCode = (HciLeMetaSubEventType)subEventCode,
            NumReports = numReports,
            Reports = reports
        };
        return true;
    }
}