using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Application.Validation;
using SlotBook.SharedKernel.Domain;
using SlotBook.SharedKernel.Infrastructure.Persistence;

namespace SlotBook.SharedKernel.Infrastructure.Messaging;

/// <summary>
/// The consumer definition every consumer derives from (AD-10): an explicit endpoint name, the retry
/// rules, then the MongoDB outbox, which is the inbox of the endpoint. Each retry gets a new transaction
/// and a new scope, because the retry filters sit outside the outbox filter. From the outside in:
/// <list type="number">
/// <item><description>Interval retry after 1, 5, 15 and 30 s. It ignores <see cref="DomainException"/>,
/// <see cref="ValidationFailedException"/> and parse errors, which no retry can fix: those go to the
/// <c>_error</c> queue at once. There is no delayed redelivery and no broker scheduler.</description></item>
/// <item><description>Conflict retry: a version conflict, write conflict or retryable duplicate key re-runs
/// the consume after a jittered backoff of 5 ms doubling to 100 ms, for about 3 s (AD-7).</description></item>
/// <item><description>The MongoDB outbox: the inbox mark, the handler's writes and the staged events commit
/// in one transaction; a redelivered message is dropped.</description></item>
/// </list>
/// A consumer is a thin adapter that dispatches one command (AD-10). A <see cref="Result{T}"/> refusal is a
/// normal outcome and returns without a throw, so the inbox mark commits.
/// </summary>
/// <typeparam name="TConsumer">The consumer this definition belongs to.</typeparam>
public abstract class SlotBookConsumerDefinition<TConsumer> : ConsumerDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    private const int ConflictRetryLimit = 30;

    private static readonly TimeSpan[] _retryIntervals =
    [
        TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30),
    ];

    /// <param name="naming">The endpoint naming of the host.</param>
    /// <param name="module">The owning module in kebab-case, for example <c>booking</c>.</param>
    /// <param name="purpose">What the endpoint does in kebab-case, for example <c>notify-on-booked</c>.</param>
    protected SlotBookConsumerDefinition(EndpointNaming naming, string module, string purpose)
    {
        ArgumentNullException.ThrowIfNull(naming);
        EndpointName = naming.Name(module, purpose);
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<TConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        ArgumentNullException.ThrowIfNull(consumerConfigurator);
        ArgumentNullException.ThrowIfNull(context);

        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Intervals(_retryIntervals);
            retry.Ignore<DomainException>();
            retry.Ignore<ValidationFailedException>();
            retry.Ignore<FormatException>();
            retry.Ignore<System.Text.Json.JsonException>();
        });

        var duplicateKeys = context.GetRequiredService<RetryableDuplicateKeys>();
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Exponential(ConflictRetryLimit, TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(5));
            retry.Handle<Exception>(exception => WriteErrorClassifier.Classify(exception, duplicateKeys) == WriteErrorAction.RerunHandler);
        });

        endpointConfigurator.UseConsumeFilter(typeof(InboxOuterLogFilter<>), context);
        endpointConfigurator.UseMongoDbOutbox(context);
        consumerConfigurator.UseFilter(new InboxInnerLogFilter<TConsumer>());
    }
}
