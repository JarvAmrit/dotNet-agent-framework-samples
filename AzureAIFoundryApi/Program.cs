using AzureAIFoundryApi.Configuration;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Bind configuration sections
builder.Services.Configure<AzureFoundrySettings>(
    builder.Configuration.GetSection(AzureFoundrySettings.SectionName));
builder.Services.Configure<ServicePrincipalSettings>(
    builder.Configuration.GetSection(ServicePrincipalSettings.SectionName));

// Register services
builder.Services.AddSingleton<CredentialFactory>();
builder.Services.AddSingleton<AIProjectClientFactory>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register health checks
builder.Services.AddHealthChecks()
    .AddCheck<FoundryHealthCheck>("ai-foundry", tags: ["ready"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Global health endpoint – returns JSON with component-level detail.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                error = e.Value.Exception?.Message
            })
        };
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
            Encoding.UTF8);
    }
});

// Liveness probe – lightweight check that confirms the process is running.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false   // no actual checks; 200 means the process is alive
});

// Readiness probe – checks that external dependencies are reachable.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
