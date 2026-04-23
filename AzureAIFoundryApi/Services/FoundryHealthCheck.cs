using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AzureAIFoundryApi.Services;

/// <summary>
/// Custom ASP.NET Core health check that verifies connectivity to Azure AI Foundry
/// by performing a lightweight read operation (listing deployments).
/// Register this check via <c>builder.Services.AddHealthChecks().AddCheck&lt;FoundryHealthCheck&gt;("ai-foundry")</c>.
/// </summary>
public class FoundryHealthCheck : IHealthCheck
{
    private readonly AIProjectClientFactory _clientFactory;
    private readonly ILogger<FoundryHealthCheck> _logger;

    public FoundryHealthCheck(AIProjectClientFactory clientFactory, ILogger<FoundryHealthCheck> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.GetClient();

            // Perform a lightweight synchronous read to verify that the Foundry
            // project endpoint is reachable and credentials are valid.
            // GetDeployments() returns an IEnumerable; iterating one item is sufficient.
            var enumerator = client.Deployments.GetDeployments().GetEnumerator();
            enumerator.MoveNext(); // Does not throw on an empty project.

            return Task.FromResult(
                HealthCheckResult.Healthy("Azure AI Foundry project endpoint is reachable."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure AI Foundry health check failed");
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "Azure AI Foundry project endpoint is unreachable.",
                    exception: ex));
        }
    }
}
