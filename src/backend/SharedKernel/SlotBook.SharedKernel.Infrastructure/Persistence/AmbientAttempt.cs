namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>The command attempt running on this async flow: its scope and the time budget it shares with its retries.</summary>
internal sealed class AmbientAttempt(IServiceProvider services, RetryBudget budget)
{
    private static readonly AsyncLocal<AmbientAttempt?> _current = new();

    public static AmbientAttempt? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public IServiceProvider Services { get; } = services;

    public RetryBudget Budget { get; } = budget;
}
