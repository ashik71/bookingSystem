using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using SlotBook.IntegrationTests.TestSupport.Jobs;
using SlotBook.SharedKernel.Infrastructure.Jobs;
using Xunit;

namespace SlotBook.IntegrationTests.Jobs;

public sealed class InternalJobEndpointTests(MongoDatabaseFixture mongo) : IClassFixture<MongoDatabaseFixture>
{
    [Fact]
    public async Task PostRunsTheJobOnceUnderItsLease()
    {
        var ct = TestContext.Current.CancellationToken;
        var control = NewControl();
        using var host = NewHost(control);
        using var client = host.CreateClient();

        await WaitForStartupRunAsync(control, ct);

        var response = await client.PostAsync("/internal/jobs/" + control.Name, null, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        control.Runs.Count.ShouldBe(2, "the startup run plus the one the POST ran");
    }

    [Fact]
    public async Task AnUnknownJobIsNotFound()
    {
        using var host = NewHost(NewControl());
        using var client = host.CreateClient();

        (await client.PostAsync("/internal/jobs/nope", null, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AHeldLeaseIsAConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var control = NewControl();
        await mongo.Database.GetCollection<BsonDocument>("leases").InsertOneAsync(
            new BsonDocument { { "_id", "job:" + control.Name }, { "holder", "other" }, { "token", "t" }, { "expiresAt", new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc) } }, cancellationToken: ct);
        using var host = NewHost(control);
        using var client = host.CreateClient();

        (await client.PostAsync("/internal/jobs/" + control.Name, null, ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TheRealAppHasNoInternalJobsRoute()
    {
        using var host = new SlotBookApiFactory
        {
            MongoDatabase = mongo.DatabaseName,
            Settings = new Dictionary<string, string?> { ["Jobs:Heartbeat:Enabled"] = "true" },
        };
        using var client = host.CreateClient();

        (await client.PostAsync("/internal/jobs/heartbeat", null, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task WaitForStartupRunAsync(RecordingJobControl control, CancellationToken ct)
    {
        while (control.Runs.IsEmpty)
        {
            await Task.Delay(25, ct);
        }
    }

    private static RecordingJobControl NewControl() => new()
    {
        Name = "recording-" + Convert.ToHexStringLower(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4)),
        Period = TimeSpan.FromHours(1),
        RunDuration = TimeSpan.FromMilliseconds(50),
    };

    private SlotBookApiFactory NewHost(RecordingJobControl control) => new()
    {
        MongoDatabase = mongo.DatabaseName,
        Settings = new Dictionary<string, string?> { ["Jobs:LeaseSeconds"] = "3" },
        ConfigureTestServices = services =>
        {
            services.AddSingleton(control);
            services.AddTimedJob<RecordingJob>();
            services.AddSingleton<IStartupFilter, MapInternalJobsFilter>();
        },
    };

#pragma warning disable CA1812 // created by the container
    private sealed class MapInternalJobsFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapInternalJobEndpoints());
            next(app);
        };
    }
}
