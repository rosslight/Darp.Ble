using System.Collections.Concurrent;
using System.Reactive.Linq;
using Darp.Ble.Hci.AssignedNumbers;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Darp.Ble.Hci.Transport;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Hci;

public sealed class AclPacketQueue(ITransportLayer transportLayer,
    int maxPacketSize,
    int maxPacketsInFlight)
{
    private readonly ITransportLayer _transportLayer = transportLayer;
    private readonly int _maxPacketSize = maxPacketSize;
    private readonly int _maxPacketsInFlight = maxPacketsInFlight;
    private readonly ConcurrentQueue<IHciPacket> _packetQueue = [];
    private int _packetsInFlight;

    /// <summary>
    /// Enqueue a new
    /// </summary>
    /// <param name="hciPacket"></param>
    public void Enqueue(IHciPacket hciPacket)
    {
        _packetQueue.Enqueue(hciPacket);
        CheckQueue();
    }

    /// <summary> Checks the number of packets in flight and send as many as possible </summary>
    private void CheckQueue()
    {
        while (_packetsInFlight < _maxPacketsInFlight && _packetQueue.TryDequeue(out IHciPacket? packet))
        {
            _transportLayer.Enqueue(packet);
            _packetsInFlight++;
        }
    }
}

/// <summary> The HCI Host </summary>
public sealed class HciHost : IDisposable
{
    private readonly ITransportLayer _transportLayer;
    internal ILogger? Logger { get; }
    private AclPacketQueue? _aclPacketQueue;

    /// <summary> Initializes a new host with a given transport layer and an optional logger </summary>
    /// <param name="transportLayer"> The transport layer </param>
    /// <param name="logger"> An optional logger </param>
    public HciHost(ITransportLayer transportLayer, ILogger? logger)
    {
        _transportLayer = transportLayer;
        Logger = logger;
        WhenHciPacketReceived = _transportLayer.WhenReceived();
        WhenHciEventPackageReceived = WhenHciPacketReceived.OfType<HciEventPacket>();
        WhenHciLeMetaEventPackageReceived = WhenHciEventPackageReceived
            .SelectWhereEvent<HciLeMetaEvent>();
    }

    /// <summary> Observable sequence of <see cref="IHciPacket"/> emitted when an HCI packet is received. </summary>
    public IObservable<IHciPacket> WhenHciPacketReceived { get; }
    /// <summary> Observable sequence of <see cref="HciEventPacket"/> emitted when an HCI event packet is received. </summary>
    public IObservable<HciEventPacket> WhenHciEventPackageReceived { get; }
    /// <summary> Observable sequence of <see cref="HciEventPacket{HciLeMetaEvent}"/> emitted when an HCI event packet with <see cref="HciLeMetaEvent"/> is received. </summary>
    public IObservable<HciEventPacket<HciLeMetaEvent>> WhenHciLeMetaEventPackageReceived { get; }

    /// <summary> Enqueue a new hci packet </summary>
    /// <param name="packet"> The packet to be enqueued </param>
    public void EnqueuePacket(IHciPacket packet)
    {
        Logger?.LogEnqueuePacket(packet);
        _transportLayer.Enqueue(packet);
    }

    /// <summary> Initialize the host </summary>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _transportLayer.Initialize();
        // Reset the controller
        await this.QueryCommandCompletionAsync<HciResetCommand, HciResetResult>(cancellationToken: cancellationToken).ConfigureAwait(false);
        // await Host.QueryCommandCompletionAsync<HciReadLocalSupportedCommandsCommand, HciReadLocalSupportedCommandsResult>();
        HciReadLocalVersionInformationResult version = await this.QueryCommandCompletionAsync<HciReadLocalVersionInformationCommand, HciReadLocalVersionInformationResult>(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (version.HciVersion < CoreVersion.BluetoothCoreSpecification42)
        {
            throw new NotSupportedException($"Controller version {version.HciVersion} is not supported. Minimum required version is 4.2");
        }
        await this.QueryCommandCompletionAsync<HciSetEventMaskCommand, HciSetEventMaskResult>(new HciSetEventMaskCommand((EventMask)0x3fffffffffffffff), cancellationToken: cancellationToken).ConfigureAwait(false);
        await this.QueryCommandCompletionAsync<HciLeSetEventMaskCommand, HciLeSetEventMaskResult>(new HciLeSetEventMaskCommand((LeEventMask)0xf0ffff), cancellationToken: cancellationToken).ConfigureAwait(false);
        HciLeReadBufferSizeResultV1 data = await this.QueryCommandCompletionAsync<HciLeReadBufferSizeCommandV1, HciLeReadBufferSizeResultV1>(cancellationToken: cancellationToken).ConfigureAwait(false);
        _aclPacketQueue = new AclPacketQueue(_transportLayer, data.LeAclDataPacketLength, data.TotalNumLeAclDataPackets);
    }

    /// <inheritdoc />
    public void Dispose() => _transportLayer.Dispose();
}