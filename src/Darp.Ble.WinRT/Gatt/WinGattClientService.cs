using System.Reactive.Disposables;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.WinRT.Gatt;

internal sealed class WinGattClientService(WinBlePeripheral peripheral, GattServiceProvider provider)
    : GattClientService(peripheral, new BleUuid(provider.Service.Uuid, inferType: true))
{
    private readonly GattServiceProvider _serviceProvider = provider;
    private readonly GattLocalService _winService = provider.Service;
    public new WinBlePeripheral Peripheral { get; } = peripheral;

    protected override async Task<IGattClientCharacteristic> CreateCharacteristicAsyncCore(BleUuid uuid,
        GattProperty gattProperty,
        IGattClientService.OnReadCallback? onRead,
        IGattClientService.OnWriteCallback? onWrite,
        CancellationToken cancellationToken)
    {
        GattLocalCharacteristicResult result = await _winService.CreateCharacteristicAsync(uuid.Value,
            new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = (GattCharacteristicProperties)gattProperty,
            })
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (result.Error is not BluetoothError.Success) throw new Exception("Nopiii");
        result.Characteristic.SubscribedClientsChanged += (sender, _) =>
        {
            foreach (GattSubscribedClient senderSubscribedClient in sender.SubscribedClients)
            {
                Peripheral.GetOrRegisterSession(senderSubscribedClient.Session);
            }
        };
        return new WinGattClientCharacteristic(this, result.Characteristic, onRead, onWrite);
    }

    public IAsyncDisposable Advertise(IAdvertisingSet advertisingSet)
    {
        AdvertisingParameters parameters = advertisingSet.Parameters;
        var winParameters = new GattServiceProviderAdvertisingParameters();
        if (parameters.Type.HasFlag(BleEventType.Connectable))
            winParameters.IsConnectable = true;
        winParameters.IsDiscoverable = true;
        if (advertisingSet.Data?.TryGetFirstType(AdTypes.ServiceData16BitUuid, out ReadOnlyMemory<byte> memory) is true)
        {
            winParameters.ServiceData = memory.ToArray().AsBuffer();
        }
        _serviceProvider.StartAdvertising(winParameters);
        return AsyncDisposable.Create(_serviceProvider, provider =>
        {
            provider.StopAdvertising();
            return ValueTask.CompletedTask;
        });
    }
}

/// <summary> An async disposable </summary>
internal sealed class AsyncDisposable(Func<ValueTask> onDispose) : IAsyncDisposable
{
    private Func<ValueTask>? _onDispose = onDispose;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Func<ValueTask>? dispose = Interlocked.Exchange(ref _onDispose, value: null);
        return dispose?.Invoke() ?? ValueTask.CompletedTask;
    }

    public static IAsyncDisposable Empty { get; } = new AsyncDisposable(() => ValueTask.CompletedTask);

    public static IAsyncDisposable Create(Func<ValueTask> onDispose) => new AsyncDisposable(onDispose);
    public static IAsyncDisposable Create(Action onDispose) => Create(onDispose, x =>
    {
        x();
        return ValueTask.CompletedTask;
    });
    public static IAsyncDisposable Create<T>(T state, Func<T, ValueTask> onDispose) => new AsyncDisposable<T>(state, onDispose);
    public static IAsyncDisposable Create<T>(T state, Action<T> onDispose) => Create((state, onDispose), x =>
    {
        x.onDispose(x.state);
        return ValueTask.CompletedTask;
    });
}

/// <summary> An async disposable </summary>
internal sealed class AsyncDisposable<T>(T state, Func<T, ValueTask> onDispose) : IAsyncDisposable
{
    private readonly T _state = state;
    private Func<T, ValueTask>? _onDispose = onDispose;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Func<T, ValueTask>? dispose = Interlocked.Exchange(ref _onDispose, value: null);
        return dispose?.Invoke(_state) ?? ValueTask.CompletedTask;
    }
}