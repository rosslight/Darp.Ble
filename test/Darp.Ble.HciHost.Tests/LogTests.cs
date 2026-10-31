using System.Reflection;
using Darp.Ble.Hci;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

/// <summary> The log events of the HCI layer </summary>
public sealed class LogTests
{
    [Fact]
    public void EveryLogEvent_HasAUniqueIdAndName_ThatTheCatalogLists()
    {
        LoggerMessageAttribute[] events =
        [
            .. typeof(HciDevice)
                .Assembly.GetTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Select(method => method.GetCustomAttribute<LoggerMessageAttribute>())
                .OfType<LoggerMessageAttribute>(),
        ];
        string catalog = File.ReadAllText(FindCatalog());

        events.ShouldNotBeEmpty();
        events.Select(e => e.EventId).ShouldBeUnique();
        events.Select(e => e.EventName).Distinct(StringComparer.Ordinal).Count().ShouldBe(events.Length);
        events.ShouldAllBe(e => catalog.Contains($"| {e.EventId} | {e.EventName} |", StringComparison.Ordinal));
    }

    private static string FindCatalog()
    {
        for (
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent
        )
        {
            string candidate = Path.Combine(directory.FullName, "docs", "telemetry.md");
            if (File.Exists(candidate))
                return candidate;
        }
        throw new FileNotFoundException("docs/telemetry.md not found above the test binaries");
    }
}
