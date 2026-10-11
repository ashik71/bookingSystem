using System.Text.Json.Serialization;
using SlotBook.Api.Errors;
using SlotBook.ServiceDefaults;
using SlotBook.SharedKernel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSharedKernel(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsSetup.Customize);
builder.Services.AddExceptionHandler<SlotBookExceptionHandler>();

var app = builder.Build();

app.UseRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages(ProblemDetailsSetup.WriteStatusCodeAsync);
app.MapDefaultEndpoints();
app.MapControllers();

app.Run();
