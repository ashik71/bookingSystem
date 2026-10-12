namespace SlotBook.SharedKernel.Infrastructure.Persistence.Leases;

/// <summary>The identity of this host in lease documents: the machine name plus a random suffix, fixed for the life of the host.</summary>
internal sealed class LeaseHolder
{
    public LeaseHolder()
    {
        Id = $"{Environment.MachineName}-{Convert.ToHexStringLower(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4))}";
    }

    public string Id { get; }

    public override string ToString() => Id;
}
