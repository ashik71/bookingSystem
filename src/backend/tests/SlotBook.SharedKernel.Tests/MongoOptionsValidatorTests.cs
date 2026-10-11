using Shouldly;
using SlotBook.SharedKernel.Infrastructure.Persistence;
using Xunit;

namespace SlotBook.SharedKernel.Tests;

public class MongoOptionsValidatorTests
{
    private const string ConnectionString = "mongodb://localhost:27017/?replicaSet=rs0";

    private readonly MongoOptionsValidator _validator = new();

    [Fact]
    public void AValidConnectionStringAndDatabaseSucceed() =>
        _validator.Validate(null, new MongoOptions { ConnectionString = ConnectionString, Database = "slotbook" }).Succeeded.ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ADatabaseIsRequired(string? database)
    {
        var result = _validator.Validate(null, new MongoOptions { ConnectionString = ConnectionString, Database = database });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull().ShouldContain("Mongo:Database");
    }

    [Theory]
    [InlineData("a.b")]
    [InlineData("a b")]
    [InlineData("a/b")]
    [InlineData("a$b")]
    public void ADatabaseNameMongoRefusesIsInvalid(string database) =>
        _validator.Validate(null, new MongoOptions { ConnectionString = ConnectionString, Database = database }).Failed.ShouldBeTrue();

    [Fact]
    public void ATooLongDatabaseNameIsInvalid() =>
        _validator.Validate(null, new MongoOptions { ConnectionString = ConnectionString, Database = new string('a', 64) }).Failed.ShouldBeTrue();

    [Fact]
    public void AMissingConnectionStringFailsFirst() =>
        _validator.Validate(null, new MongoOptions { Database = "slotbook" }).FailureMessage.ShouldNotBeNull().ShouldContain("ConnectionStrings:mongodb");
}
