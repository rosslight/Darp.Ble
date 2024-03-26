using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Logger;
using FluentAssertions;
using FluentAssertions.Reactive;

namespace Darp.Ble.Tests.Logger;

public sealed class LogEventExtensionsTests
{
    [Theory]
    [InlineData(2, null, "TestMessage", "Test1", 123)]
    public async Task Write_ShouldPublishCorrectLogEvent(int level, string? exceptionString, string messageTemplate, params object[] properties)
    {
        Exception? exception = exceptionString is null ? null : new DummyException(exceptionString);
        IObservable<LogEvent> observable = Observable.Create<LogEvent>(observer =>
        {
            observer.Write(level, exception, messageTemplate, properties);
            return Disposable.Empty;
        });

        using FluentTestObserver<LogEvent> x = observable.Observe();

        (await x.Should().PushAsync())
            .Which
            .Should()
            .HaveElementAt(0, new LogEvent
            {
                Level = level,
                Exception = exception,
                MessageTemplate = messageTemplate,
                Properties = properties,
            });
    }

    [Theory]
    [InlineData("TestMessage", "Test1", 123)]
    public async Task Verbose_ShouldPublishCorrectLogEvent(string messageTemplate, params object[] properties)
    {
        IObservable<LogEvent> observable = Observable.Create<LogEvent>(observer =>
        {
            observer.Verbose(messageTemplate, properties);
            return Disposable.Empty;
        });

        using FluentTestObserver<LogEvent> x = observable.Observe();

        (await x.Should().PushAsync())
            .Which
            .Should()
            .HaveElementAt(0, new LogEvent(0, Exception: null, messageTemplate, properties));
    }

    [Theory]
    [InlineData("TestMessage", "Test1", 123)]
    public async Task Debug_ShouldPublishCorrectLogEvent(string messageTemplate, params object[] properties)
    {
        IObservable<LogEvent> observable = Observable.Create<LogEvent>(observer =>
        {
            observer.Debug(messageTemplate, properties);
            return Disposable.Empty;
        });

        using FluentTestObserver<LogEvent> x = observable.Observe();

        (await x.Should().PushAsync())
            .Which
            .Should()
            .HaveElementAt(0, new LogEvent(1, Exception: null, messageTemplate, properties));
    }

    [Theory]
    [InlineData("TestMessage", "Test1", 123)]
    public async Task Information_ShouldPublishCorrectLogEvent(string messageTemplate, params object[] properties)
    {
        IObservable<LogEvent> observable = Observable.Create<LogEvent>(observer =>
        {
            observer.Information(messageTemplate, properties);
            return Disposable.Empty;
        });

        using FluentTestObserver<LogEvent> x = observable.Observe();

        (await x.Should().PushAsync())
            .Which
            .Should()
            .HaveElementAt(0, new LogEvent(2, Exception: null, messageTemplate, properties));
    }

    [Theory]
    [InlineData(null, "TestMessage", "Test1", 123)]
    [InlineData("Exception", "TestMessage", "Test1", 123)]
    public async Task Warning_ShouldPublishCorrectLogEvent(string? exceptionString, string messageTemplate, params object[] properties)
    {
        Exception? exception = exceptionString is null ? null : new DummyException(exceptionString);
        IObservable<LogEvent> observable = Observable.Create<LogEvent>(observer =>
        {
            if (exception is null)
                observer.Warning(messageTemplate, properties);
            else
                observer.Warning(exception, messageTemplate, properties);
            return Disposable.Empty;
        });

        using FluentTestObserver<LogEvent> x = observable.Observe();

        (await x.Should().PushAsync())
            .Which
            .Should()
            .HaveElementAt(0, new LogEvent(3, exception, messageTemplate, properties));
    }

    [Theory]
    [InlineData(null, "TestMessage", "Test1", 123)]
    [InlineData("Exception", "TestMessage", "Test1", 123)]
    public async Task Error_ShouldPublishCorrectLogEvent(string? exceptionString, string messageTemplate, params object[] properties)
    {
        Exception? exception = exceptionString is null ? null : new DummyException(exceptionString);
        IObservable<LogEvent> observable = Observable.Create<LogEvent>(observer =>
        {
            if (exception is null)
                observer.Error(messageTemplate, properties);
            else
                observer.Error(exception, messageTemplate, properties);
            return Disposable.Empty;
        });

        using FluentTestObserver<LogEvent> x = observable.Observe();

        (await x.Should().PushAsync())
            .Which
            .Should()
            .HaveElementAt(0, new LogEvent(4, exception, messageTemplate, properties));
    }

    [Theory]
    [InlineData(null, "TestMessage", "Test1", 123)]
    [InlineData("Exception", "TestMessage", "Test1", 123)]
    public async Task Fatal_ShouldPublishCorrectLogEvent(string? exceptionString, string messageTemplate, params object[] properties)
    {
        Exception? exception = exceptionString is null ? null : new DummyException(exceptionString);
        IObservable<LogEvent> observable = Observable.Create<LogEvent>(observer =>
        {
            if (exception is null)
                observer.Fatal(messageTemplate, properties);
            else
                observer.Fatal(exception, messageTemplate, properties);
            return Disposable.Empty;
        });

        using FluentTestObserver<LogEvent> x = observable.Observe();

        (await x.Should().PushAsync())
            .Which
            .Should()
            .HaveElementAt(0, new LogEvent(5, exception, messageTemplate, properties));
    }
}