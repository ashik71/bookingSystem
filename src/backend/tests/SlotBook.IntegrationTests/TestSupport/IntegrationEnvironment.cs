using Microsoft.Extensions.Configuration;

namespace SlotBook.IntegrationTests.TestSupport;

/// <summary>
/// Connection strings for the real infrastructure, bound like the API binds them
/// (<c>ConnectionStrings__mongodb</c> becomes <c>ConnectionStrings:mongodb</c>).
/// The sandbox (sandbox/run-job.sh) and CI (.github/workflows/ci.yml) set the same names.
/// </summary>
internal static class IntegrationEnvironment
{
    private static readonly IConfiguration _fromProcess = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

    public static string MongoDb => Require(_fromProcess, "mongodb");

    public static string RabbitMq => Require(_fromProcess, "rabbitmq");

    public static string Require(IConfiguration configuration, string name)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var value = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings__{name} is not set: integration tests need a real "
                + (name == "mongodb" ? "MongoDB replica set" : "RabbitMQ broker")
                + " (architecture AD-19).");
        }

        return value;
    }
}
