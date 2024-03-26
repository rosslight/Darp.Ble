using System.Reactive.Disposables;
using System.Reactive.Linq;
using Darp.Ble.Logger;
using FluentAssertions;
using FluentAssertions.Reactive;
using NSubstitute;

namespace Darp.Ble.Tests.Logger;

public sealed class LogEventExtensionsTests
{
    private readonly IObserver<LogEvent> _observer;

    public LogEventExtensionsTests() => _observer = Substitute.For<IObserver<LogEvent>>();

    [Theory]
    [InlineData(2, null, "TestMessage", "Test1", 123)]
    public async Task  Write_ShouldPublishCorrectLogEvent(int level, Exception? exception, string messageTemplate, params object[] properties)
    {
        IObservable<LogEvent> observable = Observable.Create<LogEvent>(observer =>
        {
            observer.Write(level, exception, messageTemplate, properties);
            return Disposable.Empty;
        });

        using FluentTestObserver<LogEvent> x = observable.Observe();

        (await x.Should().PushAsync())
            .Which
            .Should()
            .HaveElementAt(0, new LogEvent(level, exception, messageTemplate, properties));
    }
}