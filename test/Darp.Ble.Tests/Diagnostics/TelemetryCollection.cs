namespace Darp.Ble.Tests.Diagnostics;

/// <summary> Activity and meter listeners see the whole process, so telemetry tests run alone </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xunit names collection definitions this way"
)]
public sealed class TelemetryCollection
{
    public const string Name = "Telemetry";
}
