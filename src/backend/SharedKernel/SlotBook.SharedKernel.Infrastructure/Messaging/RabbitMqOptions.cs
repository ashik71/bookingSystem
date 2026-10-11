namespace SlotBook.SharedKernel.Infrastructure.Messaging;

/// <summary>Bound from <c>ConnectionStrings:rabbitmq</c>.</summary>
internal sealed class RabbitMqOptions
{
    public const string ConnectionStringKey = "ConnectionStrings:rabbitmq";

    public string? ConnectionString { get; set; }
}
