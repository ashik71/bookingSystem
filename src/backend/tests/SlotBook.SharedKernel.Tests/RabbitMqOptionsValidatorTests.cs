using Shouldly;
using SlotBook.SharedKernel.Infrastructure.Messaging;
using Xunit;

namespace SlotBook.SharedKernel.Tests;

public class RabbitMqOptionsValidatorTests
{
    private readonly RabbitMqOptionsValidator _validator = new();

    [Theory]
    [InlineData("amqp://guest:guest@localhost:5672/")]
    [InlineData("amqps://user:secret@broker.example.com/vhost")]
    public void AnAmqpUriSucceeds(string connectionString) =>
        _validator.Validate(null, new RabbitMqOptions { ConnectionString = connectionString }).Succeeded.ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void AConnectionStringIsRequired(string? connectionString)
    {
        var result = _validator.Validate(null, new RabbitMqOptions { ConnectionString = connectionString });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull().ShouldContain("ConnectionStrings:rabbitmq");
    }

    [Theory]
    [InlineData("localhost:5672")]
    [InlineData("http://user:secret@localhost/")]
    [InlineData("not a uri")]
    public void ANonAmqpValueIsInvalidAndIsNotEchoed(string connectionString)
    {
        var result = _validator.Validate(null, new RabbitMqOptions { ConnectionString = connectionString });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull().ShouldNotContain("secret");
        result.FailureMessage.ShouldNotContain(connectionString);
    }
}
