using Darp.Ble.Gatt;

namespace Darp.Ble.HciHost.Tests.Scenarios;

/// <summary> An adapter hosting one service, and a central connected to it, on some backend </summary>
public interface IHostScenario : IAsyncDisposable
{
    /// <summary> The adapter that hosts the service </summary>
    BleAdapter Host { get; }

    /// <summary> The central reads the service's characteristic and gets an answer </summary>
    Task<byte[]> ReadAsync();

    /// <summary> The central starts a read and does not wait for its answer </summary>
    Task StartReadAsync();

    /// <summary> The central writes the characteristic; its answer is the handler's status </summary>
    Task<GattProtocolStatus> WriteAsync(byte[] value);

    /// <summary> The central takes the link down </summary>
    Task DisconnectAsync();

    /// <summary> Whether the central got an answer to a read it started </summary>
    Task<bool> GotAnswerAsync();
}
