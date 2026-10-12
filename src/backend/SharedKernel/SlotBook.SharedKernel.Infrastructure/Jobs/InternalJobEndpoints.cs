using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace SlotBook.SharedKernel.Infrastructure.Jobs;

/// <summary>
/// <c>POST /internal/jobs/{job}</c> runs a job once through its runner (same lease, same code): 200 done, 404 unknown job,
/// 409 lease held or lost, 500 failed. The app does <b>not</b> map it: exposing it needs an ADR with an <c>internal</c>
/// auth scheme (AD-11).
/// </summary>
public static class InternalJobEndpoints
{
    public static IEndpointRouteBuilder MapInternalJobEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/internal/jobs/{job}", async (string job, IServiceProvider services, CancellationToken cancellationToken) =>
        {
            var runner = services.GetServices<JobRunner>().FirstOrDefault(candidate => candidate.JobName == job);
            if (runner is null)
            {
                return Results.NotFound();
            }

            return await runner.RunOnceAsync(cancellationToken).ConfigureAwait(false) switch
            {
                JobRunResult.Completed => Results.Ok(),
                JobRunResult.LeaseHeld or JobRunResult.LeaseLost => Results.StatusCode(StatusCodes.Status409Conflict),
                _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
            };
        });
        return endpoints;
    }
}
