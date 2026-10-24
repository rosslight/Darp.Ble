using Darp.Ble.WinRT.Gatt;
using Shouldly;

namespace Darp.Ble.WinRT.Tests;

/// <summary> Turning Windows' lists of subscribed clients into subscription changes (seam 1 of PR 7) </summary>
public sealed class SubscriptionSnapshotsTests
{
    private readonly SubscriptionSnapshots<string, string> _snapshots = new();

    private (string[] Added, string[] Removed) Update(string characteristic, string[] subscribed)
    {
        (IReadOnlyList<string> added, IReadOnlyList<string> removed) = _snapshots.Update(characteristic, subscribed);
        return ([.. added], [.. removed]);
    }

    [Fact]
    public void ANewSnapshot_ReportsWhoSubscribedAndWhoLeft()
    {
        Update("level", ["a", "b"])
            .ShouldSatisfyAllConditions(r => r.Added.ShouldBe(["a", "b"]), r => r.Removed.ShouldBe([]));

        Update("level", ["b", "c"])
            .ShouldSatisfyAllConditions(r => r.Added.ShouldBe(["c"]), r => r.Removed.ShouldBe(["a"]));
        _snapshots.IsSubscribed("level", "b").ShouldBeTrue();
        _snapshots.IsSubscribed("level", "a").ShouldBeFalse();
    }

    [Fact]
    public void TheSameSnapshotAgain_ReportsNothing()
    {
        _snapshots.Update("level", ["a"]);

        Update("level", ["a"]).ShouldSatisfyAllConditions(r => r.Added.ShouldBe([]), r => r.Removed.ShouldBe([]));
    }

    [Fact]
    public void Characteristics_AreTrackedApart()
    {
        _snapshots.Update("level", ["a"]);

        Update("other", ["a"]).ShouldSatisfyAllConditions(r => r.Added.ShouldBe(["a"]), r => r.Removed.ShouldBe([]));
        Update("other", []).ShouldSatisfyAllConditions(r => r.Added.ShouldBe([]), r => r.Removed.ShouldBe(["a"]));
        _snapshots.IsSubscribed("level", "a").ShouldBeTrue();
    }

    [Fact]
    public void AClientWhoseConnectionEnded_IsForgotten_AndCountsAsNewWhenItSubscribesAgain()
    {
        _snapshots.Update("level", ["a"]);

        _snapshots.Forget("a");

        _snapshots.IsSubscribed("level", "a").ShouldBeFalse();
        Update("level", ["a"]).ShouldSatisfyAllConditions(r => r.Added.ShouldBe(["a"]), r => r.Removed.ShouldBe([]));
    }
}
