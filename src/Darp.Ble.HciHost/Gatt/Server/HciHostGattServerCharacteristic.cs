using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Exceptions;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Att;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt.Server;

internal sealed class HciHostGattServerCharacteristic(
    GattServerService service,
    HciHostGattServerPeer serverPeer,
    BleUuid uuid,
    ushort attHandle,
    GattProperty property,
    ILogger<HciHostGattServerCharacteristic> logger) : GattServerCharacteristic(service, attHandle, uuid, property, logger)
{
    private readonly HciHostGattServerPeer _serverPeer = serverPeer;
    private readonly Dictionary<BleUuid, HciHostGattServerDescriptor> _descriptorDictionary = new();
    public ushort AttHandle { get; } = attHandle;
    internal ushort EndHandle { get; set; }
    public GattProperty Property { get; } = property;

    /// <summary>
    /// BLUETOOTH CORE SPECIFICATION Version 5.4 | Vol 3, Part G, 4.7.1 Discover All Characteristic Descriptors
    /// </summary>
    public async Task<bool> DiscoverAllDescriptorsAsync(CancellationToken token)
    {
        ushort startingHandle = AttHandle;
        while (!token.IsCancellationRequested && startingHandle < 0xFFFF)
        {
            AttReadResult response = await _serverPeer.QueryAttPduAsync<AttFindInformationReq, AttFindInformationRsp>(
                new AttFindInformationReq
                {
                    StartingHandle = startingHandle,
                    EndingHandle = EndHandle,
                }, cancellationToken: token)
                .ConfigureAwait(false);
            if (response.OpCode is AttOpCode.ATT_ERROR_RSP && AttErrorRsp
                    .TryReadLittleEndian(response.Pdu, out AttErrorRsp errorRsp, out _))
            {
                if (errorRsp.ErrorCode is AttErrorCode.AttributeNotFoundError) break;
                throw new GattCharacteristicException(this, $"Could not discover descriptors due to error {errorRsp.ErrorCode}");
            }
            if (!(response.OpCode is AttOpCode.ATT_FIND_INFORMATION_RSP && AttFindInformationRsp
                    .TryReadLittleEndian(response.Pdu, out AttFindInformationRsp rsp, out _)))
            {
                throw new GattCharacteristicException(this, $"Received unexpected att response {response.OpCode}");
            }
            if (rsp.InformationData.Length == 0) break;
            foreach ((ushort handle, ReadOnlyMemory<byte> uuid) in rsp.InformationData)
            {
                if (handle < startingHandle)
                    throw new GattCharacteristicException(this, "Handle of discovered characteristic is smaller than starting handle of service");
                var bleUuid = new BleUuid(uuid.Span);
                _descriptorDictionary[bleUuid] = new HciHostGattServerDescriptor(_serverPeer, bleUuid, handle, LoggerFactory.CreateLogger<HciHostGattServerDescriptor>());
            }
            ushort lastHandle = rsp.InformationData[^1].Handle;
            if (lastHandle == EndHandle) break;
            startingHandle = (ushort)(lastHandle + 1);
        }
        //Logger.Verbose("Discovered descriptors for characteristic {@Characteristic}", this);
        return true;
    }

    /// <inheritdoc />
    protected override async Task WriteAsyncCore(byte[] bytes, CancellationToken cancellationToken)
    {
        if (!_descriptorDictionary.TryGetValue(Uuid, out HciHostGattServerDescriptor? descriptor))
            throw new GattCharacteristicException(this, "No descriptor defining self available");
        await descriptor.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    protected override void WriteWithoutResponseCore(byte[] bytes)
    {
        if (!_descriptorDictionary.TryGetValue(Uuid, out HciHostGattServerDescriptor? descriptor))
            throw new GattCharacteristicException(this, "No descriptor defining self available");
        descriptor.WriteWithoutResponse(bytes);
    }

    protected override Task<byte[]> ReadAsyncCore(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    protected override async Task<IDisposable> EnableNotificationsAsync<TState>(TState state,
        Action<TState, byte[]> onNotify,
        CancellationToken cancellationToken)
    {
        if (!_descriptorDictionary.TryGetValue(0x2902, out HciHostGattServerDescriptor? cccd))
        {
            throw new GattCharacteristicException(this, "No cccd available");
        }
        if (!Property.HasFlag(GattProperty.Notify))
        {
            throw new GattCharacteristicException(this, "Characteristic does not support notification");
        }
        if (!await cccd.WriteAsync([0x01, 0x00], cancellationToken).ConfigureAwait(false))
        {
            throw new GattCharacteristicException(this, "Could not write notification status to cccd");
        }
        return _serverPeer.WhenAttPduReceived
            .Where(x => x.OpCode is AttOpCode.ATT_HANDLE_VALUE_NTF)
            .SelectWhere(((AttOpCode OpCode, byte[] Pdu) t, [NotNullWhen(true)] out byte[]? result) =>
            {
                if (!AttHandleValueNtf.TryReadLittleEndian(t.Pdu, out AttHandleValueNtf notification, out int _))
                {
                    result = null;
                    return false;
                }
                result = notification.Value.ToArray();
                return true;
            })
            .Subscribe(bytes => onNotify(state, bytes));
    }

    protected override async Task DisableNotificationsAsync()
    {
        if (!_descriptorDictionary.TryGetValue(0x2902, out HciHostGattServerDescriptor? cccd))
        {
            return;
        }
        await cccd.WriteAsync([0x00, 0x00], default).ConfigureAwait(false);
    }
}