using System.Buffers.Binary;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Connections;

namespace Darp.Ble.HciHost.Gatt;

/// <summary> A primary service of the peer, between its declaration and its last attribute </summary>
internal sealed class HciHostRemoteService(HciHostConnectionDriver connection, BleUuid uuid, ushort start, ushort end)
    : IRemoteServiceDriver
{
    private readonly HciHostConnectionDriver _connection = connection;
    private readonly ushort _start = start;
    private readonly ushort _end = end;

    public BleUuid Uuid { get; } = uuid;

    /// <summary> Reads the characteristic declarations page by page </summary>
    public async Task<IReadOnlyList<IRemoteCharacteristicDriver>> DiscoverCharacteristicsAsync(GattRequest request)
    {
        List<(ushort Declaration, GattProperty Properties, ushort ValueHandle, BleUuid Uuid)> declarations = [];
        ushort next = _start;
        while (next <= _end && !request.IsAbandoned)
        {
            AttReadByTypeRsp? page = await _connection
                .Client.QueryUnlessNotFoundAsync<AttReadByTypeReq<ushort>, AttReadByTypeRsp>(
                    new AttReadByTypeReq<ushort>
                    {
                        StartingHandle = next,
                        EndingHandle = _end,
                        AttributeType = 0x2803,
                    },
                    request
                )
                .ConfigureAwait(false);
            if (page is not { AttributeDataList.Length: > 0 } response)
                break;
            foreach ((ushort handle, ReadOnlyMemory<byte> value) in response.AttributeDataList)
            {
                ReadOnlySpan<byte> declaration = value.Span;
                declarations.Add(
                    (
                        handle,
                        (GattProperty)declaration[0],
                        BinaryPrimitives.ReadUInt16LittleEndian(declaration[1..]),
                        BleUuid.Read(declaration[3..])
                    )
                );
            }
            ushort last = response.AttributeDataList[^1].Handle;
            if (last >= _end)
                break;
            next = (ushort)(last + 1);
        }
        // The caller gave up: a partial list would give its characteristics wrong ends, so nothing is kept
        if (request.IsAbandoned)
            throw new OperationCanceledException("The characteristic discovery was abandoned");
        var characteristics = new IRemoteCharacteristicDriver[declarations.Count];
        for (var i = 0; i < declarations.Count; i++)
        {
            (_, GattProperty properties, ushort valueHandle, BleUuid uuid) = declarations[i];
            // A characteristic's descriptors end before the next declaration
            ushort endHandle = i + 1 < declarations.Count ? (ushort)(declarations[i + 1].Declaration - 1) : _end;
            characteristics[i] = _connection.GetCharacteristic(valueHandle, endHandle, uuid, properties);
        }
        return characteristics;
    }
}
