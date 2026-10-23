namespace Darp.Ble.Gap;

/// <summary> Counts the advertisements scans lost because their queue was full </summary>
internal sealed class ScanDropCounter
{
    private long _count;

    public long Count => Interlocked.Read(ref _count);

    public void Increment() => Interlocked.Increment(ref _count);
}
