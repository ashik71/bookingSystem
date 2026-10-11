namespace SlotBook.SharedKernel.Domain;

/// <summary>
/// Marks an aggregate that is not tenant-owned. The collection must be on the AD-5 global list;
/// any other global collection needs an ADR.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GlobalAggregateAttribute(string collection) : Attribute
{
    /// <summary>Gets the name of the global collection that stores the aggregate.</summary>
    public string Collection { get; } = collection;
}
