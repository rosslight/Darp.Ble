using System.Buffers.Binary;
using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Gatt;

namespace Darp.Ble.HciHost.Connections;

/// <summary> The link of a central that connected to this adapter's peripheral role </summary>
internal sealed class HciHostCentralLink : ICentralConnectionDriver, IDisposable
{
    private readonly IGattServerSink _sink;
    private readonly AttributeDatabase _database;
    private readonly AttServer _server;
    private readonly Lock _lock = new();

    // The value of each CCCD for this central; reset with every connection
    private readonly Dictionary<GattCharacteristicDefinition, ushort> _configurations = [];
    private ushort _serviceChangedConfiguration;
    private bool _indicating;
    private bool _changedWhileIndicating;
    private int _indication;
    private ITimer? _indicationTimer;
    private readonly TimeProvider _timeProvider;
    private readonly Action<HciHostCentralLink> _onIndicationTimeout;
    private readonly TaskCompletionSource<DisconnectInfo> _down = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    public HciHostCentralLink(
        AclConnection connection,
        BleAddress address,
        AttributeDatabase database,
        IGattServerSink sink,
        TimeProvider timeProvider,
        Action<HciHostCentralLink> onIndicationTimeout
    )
    {
        Connection = connection;
        _timeProvider = timeProvider;
        _onIndicationTimeout = onIndicationTimeout;
        Address = address;
        _database = database;
        _sink = sink;
        _server = new AttServer(this, connection, database, sink);
    }

    public BleAddress Address { get; }

    public AclConnection Connection { get; }

    /// <summary> Whether the core learned of the link; guarded by the peripheral driver </summary>
    public bool IsAnnounced { get; set; }

    /// <summary> Why the link went down before the core learned of it; guarded by the peripheral driver </summary>
    public DisconnectInfo? PendingEnd { get; set; }

    /// <summary> Whether the central left an indication unconfirmed for the ATT timeout </summary>
    public bool IndicationTimedOut { get; private set; }

    /// <summary> Completes once the link is down </summary>
    public Task<DisconnectInfo> Down => _down.Task;

    /// <summary> The central wrote a CCCD; the core learns when notifications turn on or off </summary>
    /// <returns> Whether the value fits the characteristic; one that does not leaves the CCCD as it was </returns>
    public bool Configure(GattCharacteristicDefinition characteristic, ReadOnlySpan<byte> value)
    {
        ushort configuration = BinaryPrimitives.ReadUInt16LittleEndian(value);
        bool fits =
            (configuration & ~(NotificationBit | IndicationBit)) == 0
            && ((configuration & NotificationBit) == 0 || characteristic.Properties.HasFlag(GattProperty.Notify))
            && ((configuration & IndicationBit) == 0 || characteristic.Properties.HasFlag(GattProperty.Indicate));
        if (!fits)
            return false;
        bool wasOn;
        lock (_lock)
        {
            if (_down.Task.IsCompleted)
                return true;
            wasOn = IsOn(_configurations.GetValueOrDefault(characteristic));
            _configurations[characteristic] = configuration;
        }
        bool isOn = IsOn(configuration);
        if (isOn != wasOn)
            _sink.OnSubscriptionChanged(this, characteristic, isOn);
        return true;
    }

    /// <summary> The central wrote the CCCD of Service Changed, which only indicates </summary>
    /// <returns> Whether the value fits </returns>
    public bool ConfigureServiceChanged(ReadOnlySpan<byte> value)
    {
        ushort configuration = BinaryPrimitives.ReadUInt16LittleEndian(value);
        if ((configuration & ~IndicationBit) != 0)
            return false;
        lock (_lock)
            _serviceChangedConfiguration = configuration;
        return true;
    }

    /// <summary> The CCCD value of Service Changed this central wrote </summary>
    public byte[] ServiceChangedConfiguration
    {
        get
        {
            lock (_lock)
                return [(byte)_serviceChangedConfiguration, (byte)(_serviceChangedConfiguration >> 8)];
        }
    }

    /// <summary>
    /// Tells the central that services changed, if it turned Service Changed indications on. The range covers every
    /// handle, so the central discovers again. One indication waits for its confirmation; changes meanwhile are
    /// indicated once it came, if the central still wants them. A central that does not confirm within the ATT
    /// timeout gets no more indications, and its link is taken down
    /// </summary>
    public void IndicateServiceChanged()
    {
        int indication;
        lock (_lock)
        {
            if (_down.Task.IsCompleted || (_serviceChangedConfiguration & IndicationBit) == 0)
                return;
            if (_indicating)
            {
                _changedWhileIndicating = true;
                return;
            }
            _indicating = true;
            indication = ++_indication;
        }
        _ = SendServiceChangedAsync(indication);
    }

    /// <summary> The central confirmed an indication </summary>
    public void OnIndicationConfirmed()
    {
        int indication;
        lock (_lock)
        {
            if (!_indicating || IndicationTimedOut)
                return;
            _indicationTimer?.Dispose();
            _indicationTimer = null;
            _indicating =
                _changedWhileIndicating
                && (_serviceChangedConfiguration & IndicationBit) != 0
                && !_down.Task.IsCompleted;
            _changedWhileIndicating = false;
            if (!_indicating)
                return;
            indication = ++_indication;
        }
        _ = SendServiceChangedAsync(indication);
    }

    private async Task SendServiceChangedAsync(int indication)
    {
        if (_database.ServiceChangedHandle is not { } handle)
            return;
        try
        {
            await Connection
                .SendGattPacketAsync(
                    new AttHandleValueInd { Handle = handle, Value = new byte[] { 0x01, 0x00, 0xFF, 0xFF } },
                    onHandOff: () => StartIndicationTimer(indication),
                    CancellationToken.None
                )
                .ConfigureAwait(false);
        }
        catch (Hci.Exceptions.HciException)
        {
            // The link went down; the central discovers again when it reconnects
        }
    }

    // The ATT timeout starts once the indication goes to the transport, before its confirmation can arrive
    private void StartIndicationTimer(int indication)
    {
        lock (_lock)
        {
            if (!_indicating || _indication != indication || _down.Task.IsCompleted)
                return;
            _indicationTimer = _timeProvider.CreateTimer(
                _ => OnIndicationTimeout(indication),
                state: null,
                AttTimeout,
                Timeout.InfiniteTimeSpan
            );
        }
    }

    private void OnIndicationTimeout(int indication)
    {
        lock (_lock)
        {
            if (!_indicating || _indication != indication || _down.Task.IsCompleted)
                return;
            // The indication stays outstanding, so nothing more is indicated on this link
            IndicationTimedOut = true;
        }
        _onIndicationTimeout(this);
    }

    /// <summary> The CCCD value this central wrote </summary>
    public byte[] ConfigurationOf(GattCharacteristicDefinition characteristic)
    {
        ushort configuration;
        lock (_lock)
            configuration = _configurations.GetValueOrDefault(characteristic);
        return [(byte)configuration, (byte)(configuration >> 8)];
    }

    /// <summary> Sends a notification once the controller has a buffer for it; longer values are cut to the MTU, as ATT does </summary>
    public ValueTask NotifyAsync(
        GattCharacteristicDefinition characteristic,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken
    )
    {
        // Only a central that turned notifications on gets them, as a peripheral's stack decides
        bool isOn;
        lock (_lock)
            isOn = IsOn(_configurations.GetValueOrDefault(characteristic));
        if (!isOn)
            return ValueTask.CompletedTask;
        int length = Math.Min(value.Length, Connection.AttMtu - 3);
        return Connection.SendGattPacketAsync(
            new AttHandleValueNtf { Handle = _database.ValueHandleOf(characteristic), Value = value[..length] },
            onHandOff: null,
            cancellationToken
        );
    }

    /// <summary> The link went down; returns false when it was down already </summary>
    public bool LinkDown(DisconnectInfo info) => _down.TrySetResult(info);

    private static readonly TimeSpan AttTimeout = TimeSpan.FromSeconds(30);
    private const ushort NotificationBit = 0b01;
    private const ushort IndicationBit = 0b10;

    // Notifications are on when the central set the notification bit; this backend sends no indications
    private static bool IsOn(ushort configuration) => (configuration & NotificationBit) != 0;

    public void Dispose()
    {
        lock (_lock)
            _indicationTimer?.Dispose();
        _server.Dispose();
    }
}
