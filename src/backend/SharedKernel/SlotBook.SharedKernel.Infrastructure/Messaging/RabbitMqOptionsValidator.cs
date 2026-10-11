using Microsoft.Extensions.Options;

namespace SlotBook.SharedKernel.Infrastructure.Messaging;

/// <summary>
/// Checks that the connection string is present and is an <c>amqp</c> or <c>amqps</c> URI. It does not
/// connect: the outbox lets commits succeed while the broker is down, and MassTransit keeps reconnecting.
/// </summary>
internal sealed class RabbitMqOptionsValidator : IValidateOptions<RabbitMqOptions>
{
    public ValidateOptionsResult Validate(string? name, RabbitMqOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail($"{RabbitMqOptions.ConnectionStringKey} is required");
        }

        // The URI is never echoed: it carries the broker password.
        if (!Uri.TryCreate(options.ConnectionString, UriKind.Absolute, out var uri)
            || (uri.Scheme != "amqp" && uri.Scheme != "amqps")
            || string.IsNullOrEmpty(uri.Host))
        {
            return ValidateOptionsResult.Fail($"{RabbitMqOptions.ConnectionStringKey} is not a valid amqp:// or amqps:// URI");
        }

        return ValidateOptionsResult.Success;
    }
}
