using SlotBook.SharedKernel.Domain;

namespace SlotBook.IntegrationTests.TestSupport.Persistence;

/// <summary>A global aggregate for the unit-of-work tests: no tenant field, no tenant filter.</summary>
[GlobalAggregate(CollectionName)]
internal sealed class GlobalNote : AggregateRoot
{
    public const string CollectionName = "global_notes";

    public GlobalNote(Guid id, string text)
        : base(id)
    {
        Text = text;
    }

    private GlobalNote()
        : base(Guid.Empty)
    {
    }

    public string Text { get; private set; } = string.Empty;
}
