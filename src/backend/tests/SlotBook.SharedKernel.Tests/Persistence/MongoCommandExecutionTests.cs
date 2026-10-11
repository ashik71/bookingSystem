using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Shouldly;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Application.Dispatching;
using SlotBook.SharedKernel.Application.Persistence;
using SlotBook.SharedKernel.Domain;
using SlotBook.SharedKernel.Infrastructure.Persistence;
using Xunit;

namespace SlotBook.SharedKernel.Tests.Persistence;

public class MongoCommandExecutionTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AConflictReRunsTheHandlerInAFreshScopeAndTheLastResultWins()
    {
        var time = new FakeTimeProvider();
        using var provider = new ServiceCollection().AddScoped<ScopeMarker>().BuildServiceProvider();
        var scopes = new List<ScopeMarker>();

        var result = await RunAsync(time, provider, (services, _) =>
        {
            scopes.Add(services.GetRequiredService<ScopeMarker>());
            return scopes.Count < 3
                ? throw new VersionConflictException("notes", Guid.Empty)
                : Task.FromResult<Result<int>>(scopes.Count);
        });

        result.Value.ShouldBe(3);
        scopes.Distinct().Count().ShouldBe(3);
        time.Delays.ShouldBe(2);
    }

    [Fact]
    public async Task AReRunThatFindsTheRefusalReturnsTheRefusal()
    {
        var time = new FakeTimeProvider();
        using var provider = new ServiceCollection().BuildServiceProvider();
        var runs = 0;

        var result = await RunAsync(time, provider, (_, _) =>
        {
            runs++;
            return runs == 1
                ? throw new VersionConflictException("slots", Guid.Empty)
                : Task.FromResult<Result<int>>(Result.Refused("slot_full"));
        });

        result.IsRefused.ShouldBeTrue();
        result.Code.ShouldBe("slot_full");
    }

    [Fact]
    public async Task AHandlerThatAlwaysConflictsStopsAtThreeSecondsNotAtAnAttemptCountAndAnswersConcurrencyConflict()
    {
        var time = new FakeTimeProvider();
        using var provider = new ServiceCollection().BuildServiceProvider();
        var runs = 0;

        var exception = await Should.ThrowAsync<DomainException>(() => RunAsync<int>(time, provider, (_, _) =>
        {
            runs++;
            throw new VersionConflictException("slots", Guid.Empty);
        }));

        exception.Code.ShouldBe(CommonErrors.ConcurrencyConflict);
        time.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(3));
        time.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(3.2));
        runs.ShouldBeGreaterThan(30, "the budget is time, not attempts");
    }

    [Fact]
    public async Task OtherExceptionsPassThroughWithoutARetry()
    {
        var time = new FakeTimeProvider();
        using var provider = new ServiceCollection().BuildServiceProvider();
        var runs = 0;

        await Should.ThrowAsync<InvalidOperationException>(() => RunAsync<int>(time, provider, (_, _) =>
        {
            runs++;
            throw new InvalidOperationException("boom");
        }));

        runs.ShouldBe(1);
        time.Delays.ShouldBe(0);
    }

    [Fact]
    public async Task ANestedRunJoinsTheOuterAttemptAndNeverRetriesOnItsOwn()
    {
        var time = new FakeTimeProvider();
        using var provider = new ServiceCollection().AddScoped<ScopeMarker>().BuildServiceProvider();
        var execution = new MongoCommandExecution(time, new RetryableDuplicateKeys(), NullLogger<MongoCommandExecution>.Instance);
        var outerRuns = 0;
        var innerRuns = 0;
        ScopeMarker? outerScope = null;
        ScopeMarker? innerScope = null;

        var result = await RunOnAsync(execution, provider, async (outer, token) =>
        {
            outerRuns++;
            outerScope = outer.GetRequiredService<ScopeMarker>();
            var inner = await RunOnAsync(execution, outer, (services, _) =>
            {
                innerRuns++;
                innerScope = services.GetRequiredService<ScopeMarker>();
                return outerRuns == 1
                    ? throw new VersionConflictException("notes", Guid.Empty)
                    : Task.FromResult<Result<int>>(7);
            });
            return inner;
        });

        result.Value.ShouldBe(7);
        outerRuns.ShouldBe(2, "the conflict re-ran the outer attempt");
        innerRuns.ShouldBe(2, "one inner run per outer attempt");
        innerScope.ShouldBe(outerScope);
    }

    [Fact]
    public async Task AnUnknownCommitResultIsRetriedOnTheCommitAloneAndTheHandlerRunsOnce()
    {
        var time = new FakeTimeProvider();
        using var provider = new ServiceCollection().BuildServiceProvider();
        var handlerRuns = 0;
        var commits = 0;

        var result = await RunAsync(time, provider, async (_, token) =>
        {
            handlerRuns++;
            await CommitRetry.RunAsync(
                () =>
                {
                    commits++;
                    if (commits == 1)
                    {
                        var unknown = new MongoException("unknown");
                        unknown.AddErrorLabel("UnknownTransactionCommitResult");
                        throw unknown;
                    }

                    return Task.CompletedTask;
                },
                AmbientAttempt.Current?.Budget,
                token);
            return (Result<int>)1;
        });

        result.Value.ShouldBe(1);
        handlerRuns.ShouldBe(1);
        commits.ShouldBe(2);
    }

    [Fact]
    public async Task AnUnknownCommitResultThatOutlastsTheBudgetAnswersConcurrencyConflictAndDoesNotRerunTheHandler()
    {
        var time = new FakeTimeProvider();
        using var provider = new ServiceCollection().BuildServiceProvider();
        var handlerRuns = 0;

        var exception = await Should.ThrowAsync<DomainException>(() => RunAsync<int>(time, provider, async (_, token) =>
        {
            handlerRuns++;
            await CommitRetry.RunAsync(
                () =>
                {
                    var unknown = new MongoException("unknown");
                    unknown.AddErrorLabel("UnknownTransactionCommitResult");
                    throw unknown;
                },
                AmbientAttempt.Current?.Budget,
                token);
            return 1;
        }));

        exception.Code.ShouldBe(CommonErrors.ConcurrencyConflict);
        handlerRuns.ShouldBe(1);
        time.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void BackoffCeilingDoublesFromFiveMillisecondsToAHundredMillisecondCap()
    {
        RetryBudget.CeilingFor(1).ShouldBe(TimeSpan.FromMilliseconds(5));
        RetryBudget.CeilingFor(2).ShouldBe(TimeSpan.FromMilliseconds(10));
        RetryBudget.CeilingFor(5).ShouldBe(TimeSpan.FromMilliseconds(80));
        RetryBudget.CeilingFor(6).ShouldBe(TimeSpan.FromMilliseconds(100));
        RetryBudget.CeilingFor(40).ShouldBe(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task ThePassThroughRunsTheAttemptOnceInTheCallersScope()
    {
        using var provider = new ServiceCollection().AddDispatcher().BuildServiceProvider();
        var execution = provider.GetRequiredService<ICommandExecution>();
        IServiceProvider? seen = null;

        var result = await RunOnAsync(execution, provider, (services, _) =>
        {
            seen = services;
            return Task.FromResult<Result<int>>(5);
        });

        result.Value.ShouldBe(5);
        seen.ShouldBe(provider);
    }

    private static Task<Result<T>> RunAsync<T>(FakeTimeProvider time, IServiceProvider services, Func<IServiceProvider, CancellationToken, Task<Result<T>>> attempt) =>
        RunOnAsync(new MongoCommandExecution(time, new RetryableDuplicateKeys(), NullLogger<MongoCommandExecution>.Instance), services, attempt);

    private static Task<Result<T>> RunOnAsync<T>(ICommandExecution execution, IServiceProvider services, Func<IServiceProvider, CancellationToken, Task<Result<T>>> attempt) =>
        execution.RunAsync(typeof(string), services, attempt, Ct);

#pragma warning disable CA1812 // resolved from the container
    private sealed class ScopeMarker;
#pragma warning restore CA1812
}
