using Microsoft.Extensions.Configuration;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using Xunit;

namespace SlotBook.IntegrationTests.Infrastructure;

public class IntegrationEnvironmentTests
{
    [Theory]
    [InlineData("mongodb", "ConnectionStrings__mongodb")]
    [InlineData("rabbitmq", "ConnectionStrings__rabbitmq")]
    public void MissingConnectionStringFailsWithItsName(string name, string variable)
    {
        var empty = new ConfigurationBuilder().Build();

        var ex = Should.Throw<InvalidOperationException>(() => IntegrationEnvironment.Require(empty, name));

        ex.Message.ShouldContain(variable);
    }
}
