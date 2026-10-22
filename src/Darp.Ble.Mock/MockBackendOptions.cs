namespace Darp.Ble.Mock;

/// <summary> Configures the mock backend registered by <see cref="MockBleManagerBuilderExtensions.AddMock"/> </summary>
public sealed class MockBackendOptions
{
    /// <summary> The name of the mock adapter </summary>
    public string Name { get; set; } = "Mock";
}
