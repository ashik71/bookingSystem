using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SlotBook.Api.Errors;
using SlotBook.Api.Security;
using SlotBook.ServiceDefaults;
using SlotBook.SharedKernel.Application.Dispatching;
using SlotBook.SharedKernel.Infrastructure;
using SlotBook.SharedKernel.Infrastructure.Jobs;
using SlotBook.SharedKernel.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

// The build-time OpenAPI generator (Microsoft.Extensions.ApiDescription.Server) starts this host
// without the deployment's configuration. Placeholders let it get through options validation. It
// also starts the hosted services, so those are dropped: the build needs no MongoDB or broker.
var generatingOpenApiDocument = string.Equals(System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name, "GetDocument.Insider", StringComparison.Ordinal);
if (generatingOpenApiDocument)
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:mongodb"] = "mongodb://localhost:27017/?replicaSet=rs0",
        ["ConnectionStrings:rabbitmq"] = "amqp://guest:guest@localhost:5672/",
        ["Tenancy:Tenant:Slug"] = "openapi-build",
        ["Tenancy:Tenant:DisplayName"] = "OpenAPI build",
        ["Tenancy:Tenant:Country"] = "GB",
        ["Tenancy:Tenant:TimeZone"] = "Europe/London",
        ["Tenancy:Tenant:WeekStartDay"] = "Monday",
    });
}

builder.AddServiceDefaults();
#pragma warning disable RS0030 // composition root: the one place AD-14 allows TimeProvider.System
builder.Services.AddSingleton(TimeProvider.System);
#pragma warning restore RS0030
builder.Services.AddSharedKernel(builder.Configuration);
builder.Services.AddSlotBookMessaging(builder.Configuration);
builder.Services.AddSlotBookJobs(builder.Configuration);
builder.Services.AddDispatcher();
builder.Services.AddSlotBookAntiforgery();
builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = context => throw ModelBindingErrors.ToException(context.ModelState))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Generated at build time only (AD-13); deliberately not mapped, so there is no anonymous /openapi endpoint.
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsSetup.Customize);
builder.Services.AddExceptionHandler<SlotBookExceptionHandler>();

if (generatingOpenApiDocument)
{
    builder.Services.RemoveAll<IHostedService>();
}

var app = builder.Build();

app.UseSecurityHeaders();
app.UseRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages(ProblemDetailsSetup.WriteStatusCodeAsync);
app.MapDefaultEndpoints();
app.MapControllers();

app.Run();
