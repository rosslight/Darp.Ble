using Darp.Ble.Connections;
using Darp.Ble.Gatt;
using Microsoft.Extensions.Time.Testing;

namespace Darp.Ble.HciHost.Tests.Scenarios;

/// <summary> A connection to a peripheral with one readable and writable characteristic, on some backend </summary>
public interface IScenarioPeer : IAsyncDisposable
{
    /// <summary> The clock of the manager </summary>
    FakeTimeProvider Time { get; }

    /// <summary> The connection, after discovery </summary>
    ConnectedPeripheral Connection { get; }

    /// <summary> The characteristic, read and write </summary>
    RemoteGattCharacteristic Characteristic { get; }

    /// <summary> Whether the peripheral holds its answers until <see cref="ReleaseAnswerAsync"/> </summary>
    bool HoldAnswers { get; set; }

    /// <summary> The reads and writes the peripheral got, e.g. "read" or "write 01" </summary>
    IReadOnlyList<string> Requests { get; }

    /// <summary> Waits until the peripheral got this many reads and writes </summary>
    Task WaitForRequestsAsync(int count);

    /// <summary> Sends the oldest held answer </summary>
    Task ReleaseAnswerAsync();

    /// <summary> Changes what reads return </summary>
    void SetValue(byte[] value);

    /// <summary> The peripheral takes the link down </summary>
    Task DisconnectAsync();
}
