using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;
using Darp.Ble.Implementation;

namespace Darp.Ble.WinRT;

public sealed class WinGattClientCharacteristic(
    WinGattClientService winService,
    GattLocalCharacteristic winCharacteristic)
    : IPlatformSpecificGattClientCharacteristic
{
    public WinGattClientService WinService { get; } = winService;
    private readonly GattLocalCharacteristic _winCharacteristic = winCharacteristic;

    /// <inheritdoc />
    public GattProperty Property => (GattProperty)_winCharacteristic.CharacteristicProperties;

    /// <inheritdoc />
    public IDisposable OnWrite(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback)
    {
        return Observable.FromEventPattern<TypedEventHandler<GattLocalCharacteristic, GattWriteRequestedEventArgs>,
                GattLocalCharacteristic, GattWriteRequestedEventArgs>(
                addHandler => _winCharacteristic.WriteRequested += addHandler,
                removeHandler => _winCharacteristic.WriteRequested -= removeHandler)
            .Select(pattern => Observable.FromAsync(async token =>
            {
                using Deferral deferral = pattern.EventArgs.GetDeferral();
                GattWriteRequest request = await pattern.EventArgs.GetRequestAsync().AsTask(token);
                IGattClientPeer peerClient = WinService.WinPeripheral.GetOrRegisterSession(pattern.EventArgs.Session);
                DataReader reader = DataReader.FromBuffer(request.Value);
                byte[] bytes = reader.DetachBuffer().ToArray();
                GattProtocolStatus status = await callback(peerClient, bytes, token);
                if (request.Option == GattWriteOption.WriteWithResponse)
                {
                    if (status is GattProtocolStatus.Success)
                        request.Respond();
                    else
                        request.RespondWithProtocolError((byte)status);
                }
            }))
            .Concat()
            .Subscribe();
    }

    /// <inheritdoc />
    public async Task<bool> NotifyAsync(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken)
    {
        GattSubscribedClient? subscribedClient = _winCharacteristic.SubscribedClients
            .FirstOrDefault(x => string.Equals(x.Session.DeviceId.Id, clientPeer.Address.ToString(), StringComparison.OrdinalIgnoreCase));
        if (subscribedClient is null) return false;

        GattClientNotificationResult result = await _winCharacteristic.NotifyValueAsync(source.AsBuffer(), subscribedClient).AsTask(cancellationToken);
        return result.Status is GattCommunicationStatus.Success;
    }
}

public sealed class WinGattClientService(WinBlePeripheral winPeripheral, GattLocalService winService) : IPlatformSpecificGattClientService
{
    public WinBlePeripheral WinPeripheral { get; } = winPeripheral;
    private readonly GattLocalService _winService = winService;

    public async Task<IPlatformSpecificGattClientCharacteristic> AddCharacteristicAsync(BleUuid uuid, GattProperty gattProperty, CancellationToken cancellationToken)
    {
        GattLocalCharacteristicResult result = await _winService.CreateCharacteristicAsync(uuid.Value,
            new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = (GattCharacteristicProperties)gattProperty,
            }).AsTask(cancellationToken);
        if (result.Error is not BluetoothError.Success) throw new Exception("Nopiii");
        result.Characteristic.SubscribedClientsChanged += (sender, args) =>
        {
            foreach (GattSubscribedClient senderSubscribedClient in sender.SubscribedClients)
            {
                WinPeripheral.GetOrRegisterSession(senderSubscribedClient.Session);
            }
        };
        return new WinGattClientCharacteristic(this, result.Characteristic);
    }
}

public sealed class WinBlePeripheral : IPlatformSpecificBlePeripheral
{
    private readonly Dictionary<BluetoothDeviceId, IGattClientPeer> _clients = new();
    private readonly Subject<IGattClientPeer> _whenConnected = new();
    public async Task<IPlatformSpecificGattClientService> AddServiceAsync(BleUuid uuid,
        CancellationToken cancellationToken)
    {
        GattServiceProviderResult result = await GattServiceProvider
            .CreateAsync(uuid.Value)
            .AsTask(cancellationToken);
        if (result.Error is not BluetoothError.Success) throw new Exception("Nope");
        GattServiceProvider provider = result.ServiceProvider;
        return new WinGattClientService(this, provider.Service);
    }

    public IObservable<IGattClientPeer> WhenConnected => throw new NotSupportedException("Windows does not support this feature");

    internal IGattClientPeer GetOrRegisterSession(GattSession gattSession)
    {
        if (_clients.TryGetValue(gattSession.DeviceId, out IGattClientPeer? clientPeer) && clientPeer.IsConnected)
        {
            return clientPeer;
        }
        clientPeer = new WinGattClientPeer(gattSession);
        _clients[gattSession.DeviceId] = clientPeer;
        _whenConnected.OnNext(clientPeer);
        return clientPeer;
    }
}

public sealed class WinGattClientPeer(GattSession session) : IGattClientPeer
{
    private readonly GattSession _session = session;
    public BleAddress Address { get; } = BleAddress.Parse(session.DeviceId.Id, provider: null);
    public bool IsConnected => _session.SessionStatus is GattSessionStatus.Active;
    public IObservable<Unit> WhenDisconnected => Observable
        .FromEventPattern<TypedEventHandler<GattSession,GattSessionStatusChangedEventArgs>,
            GattSession,GattSessionStatusChangedEventArgs>(
            addHandler => _session.SessionStatusChanged += addHandler,
            removeHandler => _session.SessionStatusChanged -= removeHandler)
        .Where(x => x.EventArgs.Status is GattSessionStatus.Closed)
        .Select(_ => Unit.Default);
}