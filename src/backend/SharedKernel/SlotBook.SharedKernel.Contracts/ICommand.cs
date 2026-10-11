namespace SlotBook.SharedKernel.Contracts;

/// <summary>A write request (AD-4). Handled by exactly one command handler; answers a value or a refusal.</summary>
/// <typeparam name="TResponse">The value a successful handler returns.</typeparam>
#pragma warning disable CA1040 // marker interface: it ties a request to its response type
public interface ICommand<out TResponse>
{
}
