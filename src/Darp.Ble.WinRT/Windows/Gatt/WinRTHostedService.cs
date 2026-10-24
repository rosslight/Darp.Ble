using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.WinRT.Connections;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace Darp.Ble.WinRT.Gatt;

/// <summary>
/// One added service: Windows' service provider with the characteristics and descriptors of the definition.
/// Windows creates the CCCDs itself and takes a user description as characteristic metadata
/// </summary>
internal sealed class WinRTHostedService : IDisposable
{
    private const byte UnlikelyError = 0x0E;
    private const byte InvalidOffset = 0x07;
    private static readonly BleUuid UserDescription = BleUuid.FromUInt16(0x2901);
    private static readonly BleUuid Cccd = BleUuid.FromUInt16(0x2902);

    private readonly WinRTPeripheralDriver _peripheral;
    private readonly GattServiceProvider _provider;
    private readonly Dictionary<GattCharacteristicDefinition, GattLocalCharacteristic> _characteristics = [];
    private bool _advertising;

    private WinRTHostedService(WinRTPeripheralDriver peripheral, GattServiceProvider provider)
    {
        _peripheral = peripheral;
        _provider = provider;
    }

    /// <summary> Creates the service with every characteristic and descriptor; a failure removes what was created </summary>
    public static async Task<WinRTHostedService> CreateAsync(
        WinRTPeripheralDriver peripheral,
        GattServiceDefinition definition,
        CancellationToken cancellationToken
    )
    {
        GattServiceProviderResult result = await GattServiceProvider
            .CreateAsync(definition.Uuid.Value)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (result.Error is not BluetoothError.Success)
            throw new BleException($"Windows could not create the service {definition.Uuid}: {result.Error}");
        var service = new WinRTHostedService(peripheral, result.ServiceProvider);
        try
        {
            foreach (GattCharacteristicDefinition characteristic in definition.Characteristics)
                await service.AddAsync(characteristic, cancellationToken).ConfigureAwait(false);
            return service;
        }
        catch
        {
            service.Dispose();
            throw;
        }
    }

    /// <summary> The native characteristic of an added definition, or null </summary>
    public GattLocalCharacteristic? Find(GattCharacteristicDefinition characteristic) =>
        _characteristics.GetValueOrDefault(characteristic);

    private async Task AddAsync(GattCharacteristicDefinition definition, CancellationToken cancellationToken)
    {
        var parameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = WinRTMapping.ToWindows(definition.Properties),
            ReadProtectionLevel = GattProtectionLevel.Plain,
            WriteProtectionLevel = GattProtectionLevel.Plain,
        };
        GattDescriptorDefinition? description = definition.Descriptors.FirstOrDefault(d => d.Uuid == UserDescription);
        if (description?.StaticValue is { } text)
            parameters.UserDescription = Encoding.UTF8.GetString(text.Span);
        GattLocalCharacteristicResult result = await _provider
            .Service.CreateCharacteristicAsync(definition.Uuid.Value, parameters)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        if (result.Error is not BluetoothError.Success)
            throw new BleException($"Windows could not create the characteristic {definition.Uuid}: {result.Error}");
        GattLocalCharacteristic characteristic = result.Characteristic;
        _characteristics[definition] = characteristic;
        characteristic.ReadRequested += (sender, args) =>
            _ = ReadAsync(args.Session, definition, args.GetDeferral(), args.GetRequestAsync());
        characteristic.WriteRequested += (sender, args) =>
            _ = WriteAsync(args.Session, definition, args.GetDeferral(), args.GetRequestAsync());
        characteristic.SubscribedClientsChanged += (sender, args) =>
            _ = _peripheral.OnSubscribedClientsChangedAsync(definition, sender);
        foreach (GattDescriptorDefinition descriptor in definition.Descriptors)
        {
            // Windows generates both itself
            if (descriptor.Uuid == Cccd || (descriptor.Uuid == UserDescription && descriptor.StaticValue is not null))
                continue;
            GattLocalDescriptorResult created = await characteristic
                .CreateDescriptorAsync(
                    descriptor.Uuid.Value,
                    new GattLocalDescriptorParameters
                    {
                        ReadProtectionLevel = GattProtectionLevel.Plain,
                        WriteProtectionLevel = GattProtectionLevel.Plain,
                    }
                )
                .AsTask(cancellationToken)
                .ConfigureAwait(false);
            if (created.Error is not BluetoothError.Success)
                throw new BleException($"Windows could not create the descriptor {descriptor.Uuid}: {created.Error}");
            created.Descriptor.ReadRequested += (sender, args) =>
                _ = ReadAsync(args.Session, descriptor, args.GetDeferral(), args.GetRequestAsync());
            created.Descriptor.WriteRequested += (sender, args) =>
                _ = WriteAsync(args.Session, descriptor, args.GetDeferral(), args.GetRequestAsync());
        }
    }

    /// <summary>
    /// Advertises the service; Windows serves a service to centrals only this way. Windows reports Aborted without an
    /// error on the way to Started, also when it starts again after a stop
    /// </summary>
    public async Task StartAdvertisingAsync(CancellationToken cancellationToken)
    {
        if (_advertising)
            return;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnChanged(GattServiceProvider sender, GattServiceProviderAdvertisementStatusChangedEventArgs args)
        {
            if (IsStarted(args.Status))
                started.TrySetResult();
            else if (
                args.Status is GattServiceProviderAdvertisementStatus.Aborted
                && args.Error is not BluetoothError.Success
            )
                started.TrySetException(new BleException($"Windows could not add the service: {args.Error}"));
        }
        _provider.AdvertisementStatusChanged += OnChanged;
        try
        {
            // After a stop the status still says Started, so only the events of this start count then
            bool stale = IsStarted(_provider.AdvertisementStatus);
            _provider.StartAdvertising(
                new GattServiceProviderAdvertisingParameters { IsConnectable = true, IsDiscoverable = true }
            );
            _advertising = true;
            if (!stale && IsStarted(_provider.AdvertisementStatus))
                started.TrySetResult();
            await started.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            StopAdvertising();
            throw;
        }
        finally
        {
            _provider.AdvertisementStatusChanged -= OnChanged;
        }
    }

    /// <summary> Stops advertising the service; Windows does not report this in the provider's status </summary>
    public void StopAdvertising()
    {
        if (!_advertising)
            return;
        _advertising = false;
        _provider.StopAdvertising();
    }

    private static bool IsStarted(GattServiceProviderAdvertisementStatus status) =>
        status
            is GattServiceProviderAdvertisementStatus.Started
                or GattServiceProviderAdvertisementStatus.StartedWithoutAllAdvertisementData;

    private async Task ReadAsync(
        GattSession session,
        GattAttributeDefinition attribute,
        Deferral deferral,
        IAsyncOperation<GattReadRequest> requested
    )
    {
        using (deferral)
        {
            try
            {
                WinRTCentralLink? central = await _peripheral.ResolveAsync(session).ConfigureAwait(false);
                GattReadRequest? request = await requested.AsTask().ConfigureAwait(false);
                // A session that closed, or a request Windows withdrew
                if (central is null || request is null)
                    return;
                byte[] value;
                try
                {
                    value = await _peripheral.Sink.OnReadRequest(central, attribute).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Fail(request.RespondWithProtocolError, e);
                    return;
                }
                if (request.Offset > (uint)value.Length)
                {
                    request.RespondWithProtocolError(InvalidOffset);
                    return;
                }
                request.RespondWithValue(value.AsBuffer((int)request.Offset, value.Length - (int)request.Offset));
            }
            catch (Exception e)
            {
                // An event handler must not throw into Windows; the central's request times out
                _peripheral.Logger.LogHostedRequestFailed(e);
            }
        }
    }

    private async Task WriteAsync(
        GattSession session,
        GattAttributeDefinition attribute,
        Deferral deferral,
        IAsyncOperation<GattWriteRequest> requested
    )
    {
        using (deferral)
        {
            try
            {
                WinRTCentralLink? central = await _peripheral.ResolveAsync(session).ConfigureAwait(false);
                GattWriteRequest? request = await requested.AsTask().ConfigureAwait(false);
                if (central is null || request is null)
                    return;
                byte[] value = request.Value.ToArray();
                if (request.Option is GattWriteOption.WriteWithoutResponse)
                {
                    _peripheral.Sink.OnWriteCommand(central, attribute, value);
                    return;
                }
                if (request.Offset != 0)
                {
                    request.RespondWithProtocolError(InvalidOffset);
                    return;
                }
                try
                {
                    await _peripheral.Sink.OnWriteRequest(central, attribute, value).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Fail(request.RespondWithProtocolError, e);
                    return;
                }
                request.Respond();
            }
            catch (Exception e)
            {
                // An event handler must not throw into Windows; the central's request times out
                _peripheral.Logger.LogHostedRequestFailed(e);
            }
        }
    }

    /// <summary> The core's failure as an ATT error; a connection that ends gets no answer </summary>
    private static void Fail(Action<byte> respondWithError, Exception exception)
    {
        switch (exception)
        {
            case OperationCanceledException:
                return;
            case GattException { AttStatus: { } status }:
                respondWithError((byte)status);
                return;
            default:
                respondWithError(UnlikelyError);
                return;
        }
    }

    public void Dispose() => StopAdvertising();
}
