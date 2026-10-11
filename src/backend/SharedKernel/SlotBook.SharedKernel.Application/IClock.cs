namespace SlotBook.SharedKernel.Application;

/// <summary>The only source of the current time (AD-14).</summary>
public interface IClock
{
    public DateTimeOffset UtcNow { get; }
}
