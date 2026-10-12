namespace SlotBook.SharedKernel.Infrastructure.Messaging;

/// <summary>
/// Builds endpoint (queue) names: <c>{prefix}{module}-{purpose}</c>. The prefix is
/// <c>Messaging:EndpointPrefix</c>, empty in production; tests use it to keep their queues apart.
/// </summary>
public sealed class EndpointNaming(string prefix)
{
    public string Prefix { get; } = prefix ?? string.Empty;

    public string Name(string module, string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        return $"{Prefix}{module}-{purpose}";
    }
}
