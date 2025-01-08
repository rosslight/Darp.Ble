namespace AssignedNumbersCrawler;

public sealed class ValueNameObject : INameable
{
    public required string Value { get; init; }
    public required string Name { get; init; }
}