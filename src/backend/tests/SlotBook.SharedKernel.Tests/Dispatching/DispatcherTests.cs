using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Application.Dispatching;
using SlotBook.SharedKernel.Application.Validation;
using SlotBook.SharedKernel.Contracts;
using Xunit;

namespace SlotBook.SharedKernel.Tests.Dispatching;

public class DispatcherTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FailingValidatorStopsTheHandlerAndMergesFailuresFromAllValidators()
    {
        var log = new List<string>();
        using var provider = Build(log);
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var ex = await Should.ThrowAsync<ValidationFailedException>(
            () => dispatcher.SendAsync(new Echo(string.Empty, 0), Ct));

        ex.Errors["name"].ShouldBe(["required"]);
        ex.Errors["amount"].ShouldBe(["out_of_range"]);
        log.ShouldBe(["validate-name", "validate-amount"]);
    }

    [Fact]
    public async Task ValidatorsRunBeforeTheHandlerAndSuccessReturnsTheValue()
    {
        var log = new List<string>();
        using var provider = Build(log);
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var result = await dispatcher.SendAsync(new Echo("a", 1), Ct);

        result.Value.ShouldBe("a:1");
        log.ShouldBe(["validate-name", "validate-amount", "handle"]);
    }

    [Fact]
    public async Task RefusalFromTheHandlerIsReturnedNotThrown()
    {
        using var provider = Build([]);
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var result = await dispatcher.SendAsync(new Echo("refuse", 1), Ct);

        result.IsRefused.ShouldBeTrue();
        result.Code.ShouldBe("echo_refused");
    }

    [Fact]
    public async Task QueriesGoThroughValidatorsToo()
    {
        using var provider = Build([]);
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var ex = await Should.ThrowAsync<ValidationFailedException>(() => dispatcher.QueryAsync(new Lookup(string.Empty), Ct));
        var value = await dispatcher.QueryAsync(new Lookup("x"), Ct);

        ex.Errors["key"].ShouldBe(["required"]);
        value.ShouldBe("found:x");
    }

    [Fact]
    public async Task MissingHandlerThrowsNamingTheRequestType()
    {
        using var provider = Build([]);
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => dispatcher.SendAsync(new Orphan(), Ct));

        ex.Message.ShouldContain(nameof(Orphan));
    }

    [Fact]
    public void ScanFindsInternalTypesAndDoesNotRegisterTwice()
    {
        var services = new ServiceCollection();
        services.AddUseCasesFrom(typeof(DispatcherTests).Assembly);
        services.AddUseCasesFrom(typeof(DispatcherTests).Assembly);

        services.Count(d => d.ServiceType == typeof(ICommandHandler<Echo, string>)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IQueryHandler<Lookup, string>)).ShouldBe(1);
        services.Count(d => d.ServiceType == typeof(IValidator<Echo>)).ShouldBe(2);
    }

    private static ServiceProvider Build(List<string> log)
    {
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddDispatcher();
        services.AddUseCasesFrom(typeof(DispatcherTests).Assembly);
        return services.BuildServiceProvider();
    }

    internal sealed record Echo(string Name, int Amount) : ICommand<string>;

    internal sealed record Lookup(string Key) : IQuery<string>;

    internal sealed record Orphan : ICommand<string>;

#pragma warning disable CA1812 // instantiated by dependency injection
    internal sealed class EchoNameValidator : AbstractValidator<Echo>
    {
        public EchoNameValidator(List<string> log)
        {
            RuleFor(c => c.Name).Must(_ =>
            {
                log.Add("validate-name");
                return true;
            }).WithErrorCode("unused");
            RuleFor(c => c.Name).NotEmpty();
        }
    }

    internal sealed class EchoAmountValidator : AbstractValidator<Echo>
    {
        public EchoAmountValidator(List<string> log)
        {
            RuleFor(c => c.Amount).Must(_ =>
            {
                log.Add("validate-amount");
                return true;
            }).WithErrorCode("unused");
            RuleFor(c => c.Amount).GreaterThan(0);
        }
    }

    internal sealed class LookupValidator : AbstractValidator<Lookup>
    {
        public LookupValidator()
        {
            RuleFor(q => q.Key).NotEmpty();
        }
    }

    internal sealed class EchoHandler(List<string> log) : ICommandHandler<Echo, string>
    {
        public Task<Result<string>> HandleAsync(Echo command, CancellationToken cancellationToken)
        {
            log.Add("handle");
            return Task.FromResult<Result<string>>(
                command.Name == "refuse" ? Result.Refused("echo_refused") : $"{command.Name}:{command.Amount}");
        }
    }

    internal sealed class LookupHandler : IQueryHandler<Lookup, string>
    {
        public Task<string> HandleAsync(Lookup query, CancellationToken cancellationToken) =>
            Task.FromResult($"found:{query.Key}");
    }
}
