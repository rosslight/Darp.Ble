using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Att;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt.Server;

internal sealed class HciHostGattServerCharacteristic(HciHostGattServerPeer serverPeer,
    BleUuid uuid,
    ushort attHandle,
    GattProperty property,
    ILogger? logger) : GattServerCharacteristic(uuid)
{
    private readonly HciHostGattServerPeer _serverPeer = serverPeer;
    private readonly ILogger? _logger = logger;
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
                    .TryDecode(response.Pdu, out AttErrorRsp errorRsp, out _))
            {
                if (errorRsp.ErrorCode is AttErrorCode.AttributeNotFoundError) break;
                throw new Exception($"Could not discover descriptors due to error {errorRsp.ErrorCode}");
            }
            if (!(response.OpCode is AttOpCode.ATT_FIND_INFORMATION_RSP && AttFindInformationRsp
                    .TryDecode(response.Pdu, out AttFindInformationRsp rsp, out _)))
            {
                throw new Exception($"Received unexpected att response {response.OpCode}");
            }
            if (rsp.AttributeDataList.Length == 0) break;
            foreach ((ushort handle, ushort uuid) in rsp.AttributeDataList)
            {
                if (handle < startingHandle)
                    throw new Exception("Handle of discovered characteristic is smaller than starting handle of service");
                var bleUuid = new BleUuid(uuid);
                _descriptorDictionary[bleUuid] = new HciHostGattServerDescriptor(_serverPeer, bleUuid, handle, _logger);
            }
            ushort lastHandle = rsp.AttributeDataList[^1].Handle;
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
            return;
        await descriptor.WriteWithResponseAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    protected override IConnectableObservable<byte[]> OnNotifyCore()
    {
        if (!_descriptorDictionary.TryGetValue(new BleUuid(0x2902), out HciHostGattServerDescriptor? cccd))
        {
            return Observable.Throw<byte[]>(new Exception("No cccd available")).Publish();
        }
        if (!Property.HasFlag(GattProperty.Notify))
        {
            return Observable.Throw<byte[]>(new Exception("Characteristic does not support notification")).Publish();
        }
        return Observable.Create<byte[]>(async (observer, cancellationToken) =>
        {
            IDisposable disposable = _serverPeer.WhenAttPduReceived
                .Where(x => x.OpCode is AttOpCode.ATT_HANDLE_VALUE_NTF)
                .SelectWhere(((AttOpCode OpCode, byte[] Pdu) t, [NotNullWhen(true)] out byte[]? s) =>
                {
                    if (!AttHandleValueNtf.TryDecode(t.Pdu, out AttHandleValueNtf res, out int _))
                    {
                        s = null;
                        return false;
                    }
                    s = res.Value;
                    return true;
                })
                .Subscribe(observer);
            if (!await cccd.WriteWithResponseAsync([0x01, 0x00], cancellationToken).ConfigureAwait(false))
            {
                observer.OnError(new Exception("Could not write notification status to cccd"));
                disposable.Dispose();
                return Disposable.Empty;
            }
            _logger?.LogTrace("Enabled notifications on {@Characteristic}", this);
            return disposable;
            /*return Disposable.Create(() =>
            {
                // TODO Handle correct unsubscription from notifications
                Logger.Verbose("Starting to disable notifications on {@Characteristic}", this);
                cccd.WriteWithResponseAsync([0x00, 0x00], cancellationToken)
                    .ContinueWith(_ =>
                    {
                        disposable.Dispose();
                        Logger.Verbose("Disabled notifications on {@Characteristic}", this);
                    }, cancellationToken);
            });*/
        }).Publish();
    }
}