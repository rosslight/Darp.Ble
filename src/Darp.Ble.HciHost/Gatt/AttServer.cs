using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using Darp.BinaryObjects;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Connections;
using Darp.Utils.Messaging;

namespace Darp.Ble.HciHost.Gatt;

/// <summary>
/// The ATT server of one central's link: answers its requests from the attribute database and asks the core for the
/// app's characteristics and descriptors. A central sends one request at a time, so answers keep their order
/// </summary>
internal sealed partial class AttServer : IDisposable
{
    // The longest value of an attribute
    private const int MaxValueLength = 512;

    private readonly HciHostCentralLink _link;
    private readonly AttributeDatabase _database;
    private readonly IGattServerSink _sink;
    private readonly AclConnection _connection;
    private readonly IDisposable _subscription;

    // The value a read answered in part, which Read Blob continues; a central reads one value at a time. The next read
    // (Read, Read By Type, or Read Blob at offset 0) or a write of the attribute replaces it, so a new read asks the app again
    private (ushort Handle, byte[] Value)? _longRead;

    public AttServer(
        HciHostCentralLink link,
        AclConnection connection,
        AttributeDatabase database,
        IGattServerSink sink
    )
    {
        _link = link;
        _connection = connection;
        _database = database;
        _sink = sink;
        _subscription = connection.Assembler.Subscribe(this);
    }

    private int Mtu => _connection.AttMtu;

    [MessageSink]
    private void OnReadByGroupType(AttReadByGroupTypeReq<ushort> request) =>
        ReadByGroupType(request.StartingHandle, request.EndingHandle, BleUuid.FromUInt16(request.AttributeGroupType));

    private void ReadByGroupType(ushort start, ushort end, BleUuid type)
    {
        const AttOpCode opCode = AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ;
        if (!IsValidRange(opCode, start, end))
            return;
        if (!IsGroupType(type))
        {
            Error(opCode, start, AttErrorCode.UnsupportedGroupTypeError);
            return;
        }
        List<AttGroupTypeData> services = [];
        int space = Mtu - 2;
        foreach (HostedAttribute service in InRange(start, end))
        {
            if (service.Type != type || service.StaticValue is not { } uuid)
                continue;
            // One length per answer: 16-bit and 128-bit service UUIDs go in separate answers
            if (services.Count > 0 && services[0].Value.Length != uuid.Length)
                break;
            if (space < 4 + uuid.Length)
                break;
            space -= 4 + uuid.Length;
            services.Add(new AttGroupTypeData(service.Handle, service.GroupEnd, uuid));
        }
        if (services.Count == 0)
        {
            Error(opCode, start, AttErrorCode.AttributeNotFoundError);
            return;
        }
        Answer(
            new AttReadByGroupTypeRsp
            {
                Length = (byte)(4 + services[0].Value.Length),
                AttributeDataList = [.. services],
            }
        );
    }

    [MessageSink]
    private void OnFindByTypeValue(AttFindByTypeValueReq request) => _ = FindByTypeValueAsync(request);

    private async Task FindByTypeValueAsync(AttFindByTypeValueReq request)
    {
        if (!IsValidRange(request.OpCode, request.StartingHandle, request.EndingHandle))
            return;
        var type = BleUuid.FromUInt16(request.AttributeType);
        byte[] value = request.AttributeValue.ToArray();
        List<AttFindByTypeHandlesInformation> found = [];
        int space = Mtu - 1;
        foreach (HostedAttribute attribute in InRange(request.StartingHandle, request.EndingHandle))
        {
            if (attribute.Type != type)
                continue;
            // Values the app answers for count too; one it does not allow reading matches nothing
            if (await TryReadAsync(attribute).ConfigureAwait(false) is not { } bytes || !Matches(type, bytes, value))
                continue;
            if (space < 4)
                break;
            space -= 4;
            ushort end = attribute.GroupEnd == 0 ? attribute.Handle : attribute.GroupEnd;
            found.Add(new AttFindByTypeHandlesInformation(attribute.Handle, end));
        }
        if (found.Count == 0)
        {
            Error(request.OpCode, request.StartingHandle, AttErrorCode.AttributeNotFoundError);
            return;
        }
        Answer(new AttFindByTypeValueRsp { HandlesInformationList = [.. found] });
    }

    /// <summary> A service declaration holds a UUID, which a central may ask for in 16 or 128 bits; other values match byte for byte </summary>
    private static bool Matches(BleUuid type, byte[] attributeValue, byte[] requested)
    {
        if (
            IsGroupType(type)
            && BleUuid.TryRead(attributeValue, out BleUuid? declared)
            && BleUuid.TryRead(requested, out BleUuid? asked)
        )
            return declared == asked;
        return attributeValue.AsSpan().SequenceEqual(requested);
    }

    [MessageSink]
    private void OnReadByType(AttReadByTypeReq<ushort> request) =>
        _ = ReadByTypeAsync(request.StartingHandle, request.EndingHandle, BleUuid.FromUInt16(request.AttributeType));

    private async Task ReadByTypeAsync(ushort start, ushort end, BleUuid type)
    {
        const AttOpCode opCode = AttOpCode.ATT_READ_BY_TYPE_REQ;
        if (!IsValidRange(opCode, start, end))
            return;
        HostedAttribute[] matches = [.. InRange(start, end).Where(attribute => attribute.Type == type)];
        if (matches.Length == 0)
        {
            Error(opCode, start, AttErrorCode.AttributeNotFoundError);
            return;
        }
        int maxLength = Math.Min(Mtu - 4, 253);
        List<AttReadByTypeData> data = [];
        // A value the app answers for is the only one in its answer
        if (matches[0].StaticValue is null)
        {
            if (await StartReadAsync(opCode, matches[0], maxLength).ConfigureAwait(false) is not { } value)
                return;
            data.Add(new AttReadByTypeData(matches[0].Handle, value.AsMemory(0, Math.Min(value.Length, maxLength))));
        }
        else
        {
            int space = Mtu - 2;
            foreach (HostedAttribute attribute in matches)
            {
                if (attribute.StaticValue is not { } value)
                    break;
                int length = Math.Min(value.Length, maxLength);
                if ((data.Count > 0 && data[0].Value.Length != length) || space < 2 + length)
                    break;
                space -= 2 + length;
                data.Add(new AttReadByTypeData(attribute.Handle, value.AsMemory(0, length)));
            }
        }
        Answer(new AttReadByTypeRsp { Length = (byte)(2 + data[0].Value.Length), AttributeDataList = [.. data] });
    }

    [MessageSink]
    private void OnFindInformation(AttFindInformationReq request)
    {
        if (request.StartingHandle == 0 || request.EndingHandle < request.StartingHandle)
        {
            Error(request.OpCode, request.StartingHandle, AttErrorCode.InvalidHandle);
            return;
        }
        List<AttFindInformationData> information = [];
        int space = Mtu - 2;
        foreach (HostedAttribute attribute in InRange(request.StartingHandle, request.EndingHandle))
        {
            byte[] uuid = AttUuid.ToBytes(attribute.Type);
            // One format per answer: 16-bit and 128-bit types go in separate answers
            if ((information.Count > 0 && information[0].Uuid.Length != uuid.Length) || space < 2 + uuid.Length)
                break;
            space -= 2 + uuid.Length;
            information.Add(new AttFindInformationData(attribute.Handle, uuid));
        }
        if (information.Count == 0)
        {
            Error(request.OpCode, request.StartingHandle, AttErrorCode.AttributeNotFoundError);
            return;
        }
        Answer(
            new AttFindInformationRsp
            {
                Format =
                    information[0].Uuid.Length == 2
                        ? AttFindInformationFormat.HandleAnd16BitUuid
                        : AttFindInformationFormat.HandleAnd128BitUuid,
                InformationData = information.ToArray(),
            }
        );
    }

    [MessageSink]
    private void OnRead(AttReadReq request) => _ = ReadRequestAsync(request);

    private async Task ReadRequestAsync(AttReadReq request)
    {
        if (_database.Find(request.AttributeHandle) is not { } attribute)
        {
            Error(request.OpCode, request.AttributeHandle, AttErrorCode.InvalidHandle);
            return;
        }
        if (await StartReadAsync(request.OpCode, attribute, Mtu - 1).ConfigureAwait(false) is not { } value)
            return;
        int length = Math.Min(Math.Min(value.Length, MaxValueLength), Mtu - 1);
        Answer(new AttReadRsp { AttributeValue = value.AsMemory(0, length) });
    }

    [MessageSink]
    private void OnReadBlob(AttReadBlobReq request) => _ = ReadBlobRequestAsync(request);

    private async Task ReadBlobRequestAsync(AttReadBlobReq request)
    {
        if (_database.Find(request.AttributeHandle) is not { } attribute)
        {
            Error(request.OpCode, request.AttributeHandle, AttErrorCode.InvalidHandle);
            return;
        }
        // Offset 0 starts a new read; a later offset continues the value a read started with
        byte[]? value =
            request.ValueOffset > 0 && _longRead is { } longRead && longRead.Handle == attribute.Handle
                ? longRead.Value
                : await StartReadAsync(request.OpCode, attribute, request.ValueOffset + Mtu - 1).ConfigureAwait(false);
        if (value is null)
            return;
        int length = Math.Min(value.Length, MaxValueLength);
        if (request.ValueOffset > length)
        {
            Error(request.OpCode, request.AttributeHandle, AttErrorCode.InvalidOffsetError);
            return;
        }
        int partLength = Math.Min(length - request.ValueOffset, Mtu - 1);
        Answer(new AttReadBlobRsp { PartAttributeValue = value.AsMemory(request.ValueOffset, partLength) });
    }

    [MessageSink]
    private void OnWrite(AttWriteReq request) => _ = WriteRequestAsync(request);

    private async Task WriteRequestAsync(AttWriteReq request)
    {
        if (_database.Find(request.AttributeHandle) is not { } attribute)
        {
            Error(request.OpCode, request.AttributeHandle, AttErrorCode.InvalidHandle);
            return;
        }
        if (request.AttributeValue.Length > MaxValueLength)
        {
            Error(request.OpCode, request.AttributeHandle, AttErrorCode.InvalidAttributeLengthError);
            return;
        }
        if (attribute.IsServiceChangedCccd || attribute.CccdOf is not null)
        {
            if (request.AttributeValue.Length != 2)
            {
                Error(request.OpCode, request.AttributeHandle, AttErrorCode.InvalidAttributeLengthError);
                return;
            }
            bool fits = attribute.CccdOf is { } characteristic
                ? _link.Configure(characteristic, request.AttributeValue.Span)
                : _link.ConfigureServiceChanged(request.AttributeValue.Span);
            if (!fits)
            {
                Error(request.OpCode, request.AttributeHandle, CccdImproperlyConfigured);
                return;
            }
            Answer(new AttWriteRsp());
            return;
        }
        if (attribute.Hosted is not { } hosted || !Allows(hosted, GattProperty.Write))
        {
            Error(request.OpCode, request.AttributeHandle, AttErrorCode.WriteNotPermittedError);
            return;
        }
        ForgetLongRead(attribute.Handle);
        try
        {
            // The core copies the value before it returns
            await _sink.OnWriteRequest(_link, hosted, request.AttributeValue).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Failed(request.OpCode, request.AttributeHandle, e);
            return;
        }
        Answer(new AttWriteRsp());
    }

    [MessageSink]
    private void OnWriteCommand(AttWriteCmd command)
    {
        // A command gets no answer, also not an error
        if (
            _database.Find(command.Handle) is { Hosted: { } hosted }
            && Allows(hosted, GattProperty.WriteWithoutResponse)
            && command.Value.Length <= MaxValueLength
        )
        {
            ForgetLongRead(command.Handle);
            _sink.OnWriteCommand(_link, hosted, command.Value);
        }
    }

    /// <summary> Asks the app for the value; one that fills the part sent first is kept for Read Blob to continue </summary>
    private async Task<byte[]?> StartReadAsync(AttOpCode requestOpCode, HostedAttribute attribute, int sentFirst)
    {
        byte[]? value = await ReadAsync(requestOpCode, attribute).ConfigureAwait(false);
        // A central continues after a full part, also one that ends exactly where the value ends
        _longRead =
            value is not null && Math.Min(value.Length, MaxValueLength) >= sentFirst ? (attribute.Handle, value) : null;
        return value;
    }

    /// <summary> A written value is read anew </summary>
    private void ForgetLongRead(ushort handle)
    {
        if (_longRead is { } longRead && longRead.Handle == handle)
            _longRead = null;
    }

    [MessageSink]
    private void OnUnknownPdu(L2CapPdu pdu)
    {
        if (pdu.ChannelId is not 0x0004 || pdu.Pdu.Length == 0)
            return;
        ReadOnlySpan<byte> body = pdu.Pdu[1..];
        var opCode = (AttOpCode)pdu.Pdu[0];
        // Requests with a 128-bit type
        if (opCode is AttOpCode.ATT_READ_BY_TYPE_REQ or AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ && body.Length == 20)
        {
            ushort start = BinaryPrimitives.ReadUInt16LittleEndian(body);
            ushort end = BinaryPrimitives.ReadUInt16LittleEndian(body[2..]);
            var type = BleUuid.Read(body[4..]);
            if (opCode is AttOpCode.ATT_READ_BY_TYPE_REQ)
                _ = ReadByTypeAsync(start, end, type);
            else
                ReadByGroupType(start, end, type);
            return;
        }
        if (opCode is AttOpCode.ATT_HANDLE_VALUE_CFM)
        {
            _link.OnIndicationConfirmed();
            return;
        }
        // Every other request gets an answer
        if (IsRequest(opCode))
            Error(opCode, 0x0000, AttErrorCode.RequestNotSupportedError);
    }

    /// <summary> Whether the peer waits for an answer: requests are the even opcodes without the command flag, but the confirmation </summary>
    internal static bool IsRequest(AttOpCode opCode) =>
        (byte)opCode % 2 == 0 && ((byte)opCode & 0x40) == 0 && opCode is not AttOpCode.ATT_HANDLE_VALUE_CFM;

    // The common profile error for a CCCD value the characteristic does not support
    private const AttErrorCode CccdImproperlyConfigured = (AttErrorCode)0xFD;

    private static readonly BleUuid PrimaryService = BleUuid.FromUInt16(0x2800);
    private static readonly BleUuid SecondaryService = BleUuid.FromUInt16(0x2801);

    /// <summary> Whether Read By Group Type can ask for this type: only services group attributes </summary>
    internal static bool IsGroupType(BleUuid type) => type == PrimaryService || type == SecondaryService;

    /// <summary> A range starts at handle 1 and does not end before it starts; otherwise Invalid Handle is answered </summary>
    private bool IsValidRange(AttOpCode requestOpCode, ushort start, ushort end)
    {
        if (start != 0 && end >= start)
            return true;
        Error(requestOpCode, start, AttErrorCode.InvalidHandle);
        return false;
    }

    /// <summary> The value of an attribute for this central without answering anything; null when it cannot be read </summary>
    private async Task<byte[]?> TryReadAsync(HostedAttribute attribute)
    {
        if (attribute.IsServiceChangedCccd)
            return _link.ServiceChangedConfiguration;
        if (attribute.CccdOf is { } characteristic)
            return _link.ConfigurationOf(characteristic);
        if (attribute.StaticValue is { } value)
            return value;
        if (attribute.Hosted is not { } hosted || !Allows(hosted, GattProperty.Read))
            return null;
        try
        {
            return await _sink.OnReadRequest(_link, hosted).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary> The value of an attribute for this central; null when an error was answered or nothing is to be answered </summary>
    private async Task<byte[]?> ReadAsync(AttOpCode requestOpCode, HostedAttribute attribute)
    {
        if (attribute.IsServiceChangedCccd)
            return _link.ServiceChangedConfiguration;
        if (attribute.CccdOf is { } characteristic)
            return _link.ConfigurationOf(characteristic);
        if (attribute.StaticValue is { } value)
            return value;
        if (attribute.Hosted is not { } hosted || !Allows(hosted, GattProperty.Read))
        {
            Error(requestOpCode, attribute.Handle, AttErrorCode.ReadNotPermittedError);
            return null;
        }
        try
        {
            return await _sink.OnReadRequest(_link, hosted).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Failed(requestOpCode, attribute.Handle, e);
            return null;
        }
    }

    /// <summary> Characteristics allow what their properties say; descriptors leave it to the core </summary>
    private static bool Allows(GattAttributeDefinition hosted, GattProperty property) =>
        hosted is not GattCharacteristicDefinition characteristic || characteristic.Properties.HasFlag(property);

    private void Failed(AttOpCode requestOpCode, ushort handle, Exception exception)
    {
        switch (exception)
        {
            // The central's connection ends: nothing is answered
            case OperationCanceledException:
                return;
            case GattException { AttStatus: { } status }:
                Error(requestOpCode, handle, (AttErrorCode)status);
                return;
            default:
                Error(requestOpCode, handle, AttErrorCode.UnlikelyErrorError);
                return;
        }
    }

    private IEnumerable<HostedAttribute> InRange(ushort start, ushort end)
    {
        foreach (HostedAttribute attribute in _database.Attributes)
        {
            if (attribute.Handle > end)
                yield break;
            if (attribute.Handle >= start)
                yield return attribute;
        }
    }

    private void Answer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TAttPdu>(
        TAttPdu answer
    )
        where TAttPdu : IAttPdu, IBinaryWritable => _connection.EnqueueGattPacket(answer, activity: null);

    private void Error(AttOpCode requestOpCode, ushort handle, AttErrorCode error) =>
        _connection.EnqueueGattErrorResponse(requestOpCode, handle, error, activity: null);

    public void Dispose() => _subscription.Dispose();
}
