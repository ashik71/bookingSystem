using System.Net;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using Xunit;

namespace SlotBook.IntegrationTests.Security;

public class XsrfRestartTests(MongoDatabaseFixture mongo) : IClassFixture<MongoDatabaseFixture>
{
    private const string Probe = "/test-only/security/protected";

    [Fact]
    public async Task TokenIssuedBeforeARestartIsAcceptedAfterIt()
    {
        var ct = TestContext.Current.CancellationToken;
        string cookies;
        string token;
        using (var hostA = NewHost(mongo.DatabaseName))
        {
            var issue = await NewClient(hostA).GetAsync("/api/xsrf-token", ct);
            cookies = XsrfCookies.CookieHeader(issue);
            token = XsrfCookies.RequestToken(issue);
        }

        using var hostB = NewHost(mongo.DatabaseName);
        using var request = PostWith(cookies, token);
        var response = await NewClient(hostB).SendAsync(request, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await mongo.Database.GetCollection<BsonDocument>("data_protection_keys").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: ct))
            .ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task TokenIssuedBeforeARestartIsRefusedWhenTheKeysAreNotInTheDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        string cookies;
        string token;
        using (var hostA = NewHost(mongo.DatabaseName))
        {
            var issue = await NewClient(hostA).GetAsync("/api/xsrf-token", ct);
            cookies = XsrfCookies.CookieHeader(issue);
            token = XsrfCookies.RequestToken(issue);
        }

        var freshDatabase = MongoDatabaseFixture.NewDatabaseName("XsrfFresh");
        try
        {
            using var hostB = NewHost(freshDatabase);
            using var request = PostWith(cookies, token);
            var response = await NewClient(hostB).SendAsync(request, ct);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
        finally
        {
            await mongo.Client.DropDatabaseAsync(freshDatabase, ct);
        }
    }

    [Fact]
    public void KeysAreStoredThroughTheMongoRepository()
    {
        using var host = NewHost(mongo.DatabaseName);

        var options = host.Services.GetRequiredService<IOptions<KeyManagementOptions>>().Value;

        options.XmlRepository.ShouldNotBeNull().GetType().Name.ShouldBe("MongoXmlRepository");
    }

    private static SlotBookApiFactory NewHost(string database) => new() { MongoDatabase = database };

    private static HttpClient NewClient(SlotBookApiFactory host) =>
        host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = false });

    private static HttpRequestMessage PostWith(string cookies, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Probe);
        request.Headers.Add("Cookie", cookies);
        request.Headers.Add("X-XSRF-TOKEN", token);
        return request;
    }
}
