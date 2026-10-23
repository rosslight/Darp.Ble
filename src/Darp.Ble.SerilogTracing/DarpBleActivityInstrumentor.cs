using System.Diagnostics;
using System.Text;
using Darp.Ble.Diagnostics;
using Serilog.Events;
using Serilog.Parsing;
using SerilogTracing.Instrumentation;

namespace Darp.Ble.SerilogTracing;

/// <summary> Renders the library's spans with their names as message templates, filled from their tags </summary>
/// <remarks>
/// A span name such as <c>Read {Uuid}</c> takes the value of the tag <c>darp.ble.uuid</c>; <c>{Operation}</c> the tag
/// <c>darp.ble.operation</c>. Register it with <c>new ActivityListenerConfiguration().Instrument.With&lt;DarpBleActivityInstrumentor&gt;()</c>.
/// SerilogTracing also adds each filled property as a tag of the span
/// </remarks>
public sealed class DarpBleActivityInstrumentor : ActivitySourceInstrumentor
{
    private const string TagPrefix = "darp.ble.";

    private static readonly MessageTemplateParser Parser = new();

    /// <inheritdoc />
    public override bool ShouldSubscribeTo(string activitySourceName)
    {
        ArgumentNullException.ThrowIfNull(activitySourceName);
        // The core's source and those of the backends below it, e.g. Darp.Ble.HciHost; not Darp.Bleeding
        return activitySourceName == BleTelemetry.SourceName
            || activitySourceName.StartsWith(BleTelemetry.SourceName + ".", StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override void InstrumentOnActivityStarted(Activity activity) { }

    /// <inheritdoc />
    public override void InstrumentOnActivityStopped(Activity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        MessageTemplate template = Parser.Parse(activity.DisplayName);
        ActivityInstrumentation.SetMessageTemplateOverride(activity, template);
        foreach (PropertyToken property in template.Tokens.OfType<PropertyToken>())
        {
            if (activity.GetTagItem(TagPrefix + ToTagName(property.PropertyName)) is { } value)
                ActivityInstrumentation.SetLogEventProperty(activity, property.PropertyName, new ScalarValue(value));
        }
    }

    /// <summary> The tag name of a template property: <c>Uuid</c> is <c>uuid</c>, <c>RemoteAddress</c> is <c>remote_address</c> </summary>
    private static string ToTagName(string propertyName)
    {
        var name = new StringBuilder(propertyName.Length + 4);
        for (var index = 0; index < propertyName.Length; index++)
        {
            char character = propertyName[index];
            if (char.IsUpper(character) && index > 0)
                name.Append('_');
            name.Append(char.ToLowerInvariant(character));
        }
        return name.ToString();
    }
}
