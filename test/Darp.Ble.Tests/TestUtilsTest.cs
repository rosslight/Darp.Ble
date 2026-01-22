using System.Reactive.Linq;
using Darp.Ble.Tests.TestUtils;
using Shouldly;

namespace Darp.Ble.Tests;

public sealed class TestUtilsTest
{
    [Fact]
    public async Task Test()
    {
        IObservable<int> observable = Observable.Range(1, 5);

        await Assert.ThrowsAsync<ShouldAssertException>(async () => await observable.ShouldPushAtLeastAsync(10));
    }

    [Fact]
    public async Task TestFoo()
    {
        IObservable<int> observable = Observable.Range(1, 50);

        await observable.ShouldPushAtLeastAsync(10);
    }
}
