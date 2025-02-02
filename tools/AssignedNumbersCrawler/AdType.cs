namespace AssignedNumbersCrawler;

public sealed class AdType : INameable
{
    public required string Value { get; init; }
    public required string Name { get; init; }
    public required string Reference { get; init; }
}
