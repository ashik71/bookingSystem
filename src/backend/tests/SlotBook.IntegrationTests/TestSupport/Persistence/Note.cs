using SlotBook.SharedKernel.Domain;

namespace SlotBook.IntegrationTests.TestSupport.Persistence;

/// <summary>A tenant-owned aggregate for the unit-of-work tests.</summary>
internal sealed class Note : TenantAggregateRoot
{
    public Note(Guid id, string text)
        : base(id)
    {
        Text = text;
    }

    // The rehydration constructor the driver uses.
    private Note()
        : base(Guid.Empty)
    {
    }

    public string Text { get; private set; } = string.Empty;

    public NoteKind Kind { get; private set; } = NoteKind.Plain;

    public void Edit(string text, NoteKind kind)
    {
        Text = text;
        Kind = kind;
    }
}
