using System.Runtime.InteropServices.WindowsRuntime;
using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.WinRT.Connections;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace Darp.Ble.WinRT.Gap;

/// <summary>
/// One advertising set; the core serializes its calls and changes parameters only while it is stopped. Windows aborts a
/// running publisher for good when the radio turns off; the set reports that to the core
/// </summary>
internal sealed class WinRTAdvertisingSetDriver(
    WinRTPeripheralDriver? peripheral,
    AdvertisingParameters parameters,
    AdvertisingData data,
    IAdvertisingSetSink sink,
    ILogger logger
) : IAdvertisingSetDriver
{
#pragma warning disable CA2213 // The adapter owns the peripheral
    private readonly WinRTPeripheralDriver? _peripheral = peripheral;
#pragma warning restore CA2213
    private readonly IAdvertisingSetSink _sink = sink;
    private readonly ILogger _logger = logger;

    // The core serializes the calls, so the fields below need no lock
    private AdvertisingParameters _parameters = parameters;
    private AdvertisingData _data = data;
    private BluetoothLEAdvertisementPublisher? _publisher;
    private bool _servicesAdvertised;

    /// <summary> Windows chooses the address </summary>
    public BleAddress RandomAddress => BleAddress.NotAvailable;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_parameters.Type.HasFlag(BleEventType.Connectable))
        {
            if (_peripheral is not { HasServices: true } peripheral)
                throw new NotSupportedException(
                    "Windows advertises connectable only the added services; add one first"
                );
            if (_data.Count > 0)
                throw new NotSupportedException(
                    "Windows advertises connectable only the added services, without data of the app's own"
                );
            await peripheral.SetAdvertisedAsync(true, cancellationToken).ConfigureAwait(false);
            _servicesAdvertised = true;
            return;
        }
        // The core starts only a stopped set, so a publisher still here was aborted and reported; a late duplicate of
        // that report must not reach the new run
        _publisher = null;
        Publish(await StartPublisherAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary> A publisher with the current data, once Windows advertises it; a failed start leaves nothing running </summary>
    private async Task<BluetoothLEAdvertisementPublisher> StartPublisherAsync(CancellationToken cancellationToken)
    {
        BluetoothLEAdvertisementPublisher publisher = CreatePublisher();
        Task started = WaitForAsync(publisher, BluetoothLEAdvertisementPublisherStatus.Started, cancellationToken);
        publisher.Start();
        try
        {
            await started.ConfigureAwait(false);
            publisher.StatusChanged += OnRunningStatusChanged;
            return publisher;
        }
        catch
        {
            // Windows may still start it after a cancel
            if (
                publisher.Status
                is BluetoothLEAdvertisementPublisherStatus.Waiting
                    or BluetoothLEAdvertisementPublisherStatus.Started
            )
                publisher.Stop();
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_servicesAdvertised && _peripheral is { } peripheral)
        {
            // Still this set's share until the stop went through, so a cancelled stop can be repeated
            await peripheral.SetAdvertisedAsync(false, cancellationToken).ConfigureAwait(false);
            _servicesAdvertised = false;
        }
        if (_publisher is { } publisher)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _publisher = null;
            await StopPublisherAsync(publisher).ConfigureAwait(false);
        }
    }

    /// <summary> Once asked to stop, Windows stops, so the wait for it is not cancellable: the set never looks advertising when it is not </summary>
    private async Task StopPublisherAsync(BluetoothLEAdvertisementPublisher publisher)
    {
        publisher.StatusChanged -= OnRunningStatusChanged;
        if (
            publisher.Status
            is not BluetoothLEAdvertisementPublisherStatus.Started
                and not BluetoothLEAdvertisementPublisherStatus.Waiting
        )
            return;
        Task stopped = WaitForAsync(publisher, BluetoothLEAdvertisementPublisherStatus.Stopped, CancellationToken.None);
        publisher.Stop();
        await stopped.ConfigureAwait(false);
    }

    public Task SetParametersAsync(AdvertisingParameters parameters, CancellationToken cancellationToken)
    {
        _parameters = parameters;
        return Task.CompletedTask;
    }

    /// <summary>
    /// A running publisher takes no new data, so a new one starts before the old one stops: when starting fails, the
    /// set keeps advertising its old data
    /// </summary>
    public async Task SetDataAsync(AdvertisingData data, CancellationToken cancellationToken)
    {
        if (_parameters.Type.HasFlag(BleEventType.Connectable) && data.Count > 0)
            throw new NotSupportedException(
                "Windows advertises connectable only the added services, without data of the app's own"
            );
        AdvertisingData previous = _data;
        _data = data;
        // An aborted publisher was reported to the core as stopped; the data apply at the next start
        if (_publisher is not { Status: not BluetoothLEAdvertisementPublisherStatus.Aborted } running)
            return;
        BluetoothLEAdvertisementPublisher started;
        try
        {
            started = await StartPublisherAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _data = previous;
            throw;
        }
        Publish(started);
        // The new data apply already; the old publisher only has to go
        await StopPublisherAsync(running).ConfigureAwait(false);
    }

    public Task SetScanResponseDataAsync(AdvertisingData data, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Windows sends no scan response data of the app's own");

    public Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Windows chooses the advertising address itself");

    public ValueTask DisposeAsync() => new(StopAsync(CancellationToken.None));

    private BluetoothLEAdvertisementPublisher CreatePublisher()
    {
        var publisher = new BluetoothLEAdvertisementPublisher
        {
            UseExtendedAdvertisement = !_parameters.Type.HasFlag(BleEventType.Legacy),
        };
        if (_parameters.AdvertisingTxPower is not TxPowerLevel.NotAvailable)
        {
            publisher.IncludeTransmitPowerLevel = true;
            publisher.PreferredTransmitPowerLevelInDBm = (short)_parameters.AdvertisingTxPower;
        }
        (IReadOnlyList<(AdTypes Type, ReadOnlyMemory<byte> Bytes)> kept, IReadOnlyList<AdTypes> dropped) =
            PublisherSections.Split(_data);
        foreach (AdTypes type in dropped)
            _logger.LogReservedSectionDropped((byte)type);
        foreach ((AdTypes type, ReadOnlyMemory<byte> bytes) in kept)
            publisher.Advertisement.DataSections.Add(
                new BluetoothLEAdvertisementDataSection((byte)type, bytes.ToArray().AsBuffer())
            );
        return publisher;
    }

    /// <summary> Makes a started publisher the set's; an abort it ignored while it was not the set's yet is reported now </summary>
    private void Publish(BluetoothLEAdvertisementPublisher publisher)
    {
        _publisher = publisher;
        if (publisher.Status is BluetoothLEAdvertisementPublisherStatus.Aborted)
            ReportAborted(publisher, null);
    }

    /// <summary> Only a started publisher has this handler, and a stop the core asks for removes it first </summary>
    private void OnRunningStatusChanged(
        BluetoothLEAdvertisementPublisher sender,
        BluetoothLEAdvertisementPublisherStatusChangedEventArgs args
    )
    {
        if (args.Status is BluetoothLEAdvertisementPublisherStatus.Aborted)
            ReportAborted(sender, args.Error);
    }

    /// <summary> A publisher this set replaced, or has not taken yet, reports nothing for the current one </summary>
    private void ReportAborted(BluetoothLEAdvertisementPublisher publisher, BluetoothError? error)
    {
        if (!ReferenceEquals(publisher, _publisher))
            return;
        publisher.StatusChanged -= OnRunningStatusChanged;
        _sink.OnFailed(Stopped(error));
    }

    /// <summary> Without an error, Windows aborted the publisher before the set took it; Windows reports no reason then </summary>
    private static BleException Stopped(BluetoothError? error) =>
        new(
            error is null ? "Windows stopped advertising" : $"Windows stopped advertising: {error}",
            isTransient: error is null or BluetoothError.RadioNotAvailable,
            nativeCode: null,
            innerException: null
        );

    /// <summary> Waits for the publisher's status; Aborted fails with Windows' error </summary>
    private static async Task WaitForAsync(
        BluetoothLEAdvertisementPublisher publisher,
        BluetoothLEAdvertisementPublisherStatus status,
        CancellationToken cancellationToken
    )
    {
        var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnChanged(
            BluetoothLEAdvertisementPublisher sender,
            BluetoothLEAdvertisementPublisherStatusChangedEventArgs args
        )
        {
            if (args.Status == status)
                reached.TrySetResult();
            else if (args.Status is BluetoothLEAdvertisementPublisherStatus.Aborted)
                reached.TrySetException(Stopped(args.Error));
        }
        publisher.StatusChanged += OnChanged;
        try
        {
            await reached.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            publisher.StatusChanged -= OnChanged;
        }
    }
}
