namespace SlotBook.SharedKernel.Contracts;

/// <summary>A read request (AD-4). Handled by exactly one query handler.</summary>
/// <typeparam name="TResponse">The value the handler returns.</typeparam>
#pragma warning disable CA1040 // marker interface: it ties a request to its response type
public interface IQuery<out TResponse>
{
}
