namespace Darp.Ble.Logger;

/// <summary> Log event wrapper </summary>
/// <param name="Level"> The level of the event </param>
/// <param name="Exception"> An optional exception </param>
/// <param name="MessageTemplate"> The message template </param>
/// <param name="Properties"> Optional properties which belong to the message template </param>
public readonly record struct LogEvent(int Level, Exception? Exception, string MessageTemplate, object?[] Properties);