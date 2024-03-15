
namespace Darp.Ble.Logger;

/// <summary> A class with extensions used to work with the logger observable </summary>
public static class LogExtensions
{
    /// <summary> Write a generic log event </summary>
    /// <param name="observer"> The observer to publish the event to </param>
    /// <param name="level"> The level of the event </param>
    /// <param name="messageTemplate"> The message template </param>
    /// <param name="properties"> Optional properties which belong to the message template </param>
    public static void Write(this IObserver<LogEvent> observer, int level, string messageTemplate, params object?[] properties)
    {
        observer.OnNext(new LogEvent(level, null, messageTemplate, properties));
    }

    /// <summary> Write a verbose log event </summary>
    /// <param name="observer"> The observer to publish the event to </param>
    /// <param name="messageTemplate"> The message template </param>
    /// <param name="properties"> Optional properties which belong to the message template </param>
    public static void Verbose(this IObserver<LogEvent> observer, string messageTemplate, params object?[] properties)
    {
        observer.OnNext(new LogEvent(0, null, messageTemplate, properties));
    }

    /// <summary> Write a debug log event </summary>
    /// <param name="observer"> The observer to publish the event to </param>
    /// <param name="messageTemplate"> The message template </param>
    /// <param name="properties"> Optional properties which belong to the message template </param>
    public static void Debug(this IObserver<LogEvent> observer, string messageTemplate, params object?[] properties)
    {
        observer.OnNext(new LogEvent(1, null, messageTemplate, properties));
    }

    /// <summary> Write a information log event </summary>
    /// <param name="observer"> The observer to publish the event to </param>
    /// <param name="messageTemplate"> The message template </param>
    /// <param name="properties"> Optional properties which belong to the message template </param>
    public static void Information(this IObserver<LogEvent> observer, string messageTemplate, params object?[] properties)
    {
        observer.OnNext(new LogEvent(2, null, messageTemplate, properties));
    }

    /// <summary> Write a warn log event </summary>
    /// <param name="observer"> The observer to publish the event to </param>
    /// <param name="messageTemplate"> The message template </param>
    /// <param name="properties"> Optional properties which belong to the message template </param>
    public static void Warning(this IObserver<LogEvent> observer, string messageTemplate, params object?[] properties)
    {
        observer.OnNext(new LogEvent(3, null, messageTemplate, properties));
    }

    /// <summary> Write a warn log event </summary>
    /// <param name="observer"> The observer to publish the event to </param>
    /// <param name="exception"> The exception which lead to the event </param>
    /// <param name="messageTemplate"> The message template </param>
    /// <param name="properties"> Optional properties which belong to the message template </param>
    public static void Warning(this IObserver<LogEvent> observer, Exception exception, string messageTemplate, params object?[] properties)
    {
        observer.OnNext(new LogEvent(3, exception, messageTemplate, properties));
    }

    /// <summary> Write an error log event </summary>
    /// <param name="observer"> The observer to publish the event to </param>
    /// <param name="messageTemplate"> The message template </param>
    /// <param name="properties"> Optional properties which belong to the message template </param>
    public static void Error(this IObserver<LogEvent> observer, string messageTemplate, params object?[] properties)
    {
        observer.OnNext(new LogEvent(4, null, messageTemplate, properties));
    }

    /// <summary> Write an error log event </summary>
    /// <param name="observer"> The observer to publish the event to </param>
    /// <param name="exception"> The exception which lead to the event </param>
    /// <param name="messageTemplate"> The message template </param>
    /// <param name="properties"> Optional properties which belong to the message template </param>
    public static void Error(this IObserver<LogEvent> observer, Exception exception, string messageTemplate, params object?[] properties)
    {
        observer.OnNext(new LogEvent(4, exception, messageTemplate, properties));
    }

    /// <summary> Write a fatal log event </summary>
    /// <param name="observer"> The observer to publish the event to </param>
    /// <param name="messageTemplate"> The message template </param>
    /// <param name="properties"> Optional properties which belong to the message template </param>
    public static void Fatal(this IObserver<LogEvent> observer, string messageTemplate, params object?[] properties)
    {
        observer.OnNext(new LogEvent(5, null, messageTemplate, properties));
    }

    /// <summary> Write a fatal log event </summary>
    /// <param name="observer"> The observer to publish the event to </param>
    /// <param name="exception"> The exception which lead to the event </param>
    /// <param name="messageTemplate"> The message template </param>
    /// <param name="properties"> Optional properties which belong to the message template </param>
    public static void Fatal(this IObserver<LogEvent> observer, Exception exception, string messageTemplate, params object?[] properties)
    {
        observer.OnNext(new LogEvent(5, exception, messageTemplate, properties));
    }
}