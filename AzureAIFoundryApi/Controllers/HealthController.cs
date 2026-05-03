using System.ClientModel;
using Azure.AI.Projects;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryApi.Controllers;

/// <summary>
/// Controller for health and liveness checks of the API and individual agents.
/// Use these endpoints for monitoring, alerting, and operational audits.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AIProjectClientFactory _clientFactory;
    private readonly ILogger<HealthController> _logger;

    public HealthController(AIProjectClientFactory clientFactory, ILogger<HealthController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Returns the overall health of the API and its connectivity to Azure AI Foundry.
    /// Performs a lightweight probe (lists one deployment) to verify the Foundry
    /// project endpoint is reachable and credentials are valid.
    /// </summary>
    /// <param name="projectEndpoint">The Azure AI Foundry project endpoint URL (e.g., https://your-project.services.ai.azure.com).</param>
    [HttpGet]
    public async Task<IActionResult> GetHealth([FromQuery] string projectEndpoint)
    {
        var response = new HealthResponse
        {
            Timestamp = DateTimeOffset.UtcNow
        };

        var foundryCheck = new HealthCheckResult { Name = "FoundryConnectivity" };

        try
        {
            var client = _clientFactory.GetClient(projectEndpoint);

            // Lightweight probe: attempt to list one deployment to confirm connectivity
            await foreach (var _ in client.Deployments.GetDeploymentsAsync())
                break;

            foundryCheck.Status = "Healthy";
            foundryCheck.Description = "Successfully connected to the Azure AI Foundry project endpoint.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Foundry connectivity health check failed");
            foundryCheck.Status = "Unhealthy";
            foundryCheck.Description = "Failed to connect to the Azure AI Foundry project endpoint.";
            foundryCheck.Error = ex.Message;
        }

        response.Checks.Add(foundryCheck);
        response.Status = foundryCheck.Status;

        var statusCode = response.Status == "Healthy" ? 200 : 503;
        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Returns the health status of a specific agent by name.
    /// Checks whether the agent exists in the Foundry catalog and reports its
    /// identifier. Use this for per-agent audit and monitoring.
    /// </summary>
    /// <param name="agentName">The name of the agent to check.</param>
    /// <param name="projectEndpoint">The Azure AI Foundry project endpoint URL (e.g., https://your-project.services.ai.azure.com).</param>
    [HttpGet("agents/{agentName}")]
    public async Task<IActionResult> GetAgentHealth(string agentName, [FromQuery] string projectEndpoint)
    {
        var response = new AgentHealthResponse
        {
            AgentName = agentName,
            Timestamp = DateTimeOffset.UtcNow
        };

        var catalogCheck = new HealthCheckResult { Name = "AgentCatalogAvailability" };
        var latestVersionCheck = new HealthCheckResult { Name = "AgentLatestVersionAvailability" };

        // --- Catalog check: verify the agent record exists ---
        try
        {
            var client = _clientFactory.GetClient(projectEndpoint);
            var agentAdmin = client.AgentAdministrationClient;
            var result = await agentAdmin.GetAgentAsync(agentName);
            var agent = result.Value;

            response.AgentId = agent.Id;

            catalogCheck.Status = "Healthy";
            catalogCheck.Description = $"Agent '{agentName}' is registered in the Foundry catalog (id: {agent.Id}).";
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            catalogCheck.Status = "Unhealthy";
            catalogCheck.Description = $"Agent '{agentName}' was not found in the Foundry catalog.";
            catalogCheck.Error = "Agent not found (HTTP 404).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Catalog health check failed for agent '{AgentName}'", Sanitize(agentName));
            catalogCheck.Status = "Unhealthy";
            catalogCheck.Description = $"Failed to retrieve agent '{agentName}' from the Foundry catalog.";
            catalogCheck.Error = ex.Message;
        }

        // --- Latest version check: verify at least one version is deployed ---
        try
        {
            var client = _clientFactory.GetClient(projectEndpoint);
            var agentAdmin = client.AgentAdministrationClient;
            bool hasVersion = false;
            await foreach (var _ in agentAdmin.GetAgentVersionsAsync(agentName))
            {
                hasVersion = true;
                break;
            }

            if (hasVersion)
            {
                latestVersionCheck.Status = "Healthy";
                latestVersionCheck.Description = $"Agent '{agentName}' has at least one deployed version.";
            }
            else
            {
                latestVersionCheck.Status = "Degraded";
                latestVersionCheck.Description = $"Agent '{agentName}' has no deployed versions.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Version health check failed for agent '{AgentName}'", Sanitize(agentName));
            latestVersionCheck.Status = "Unhealthy";
            latestVersionCheck.Description = $"Failed to list versions for agent '{agentName}'.";
            latestVersionCheck.Error = ex.Message;
        }

        response.Checks.Add(catalogCheck);
        response.Checks.Add(latestVersionCheck);

        // Aggregate: Unhealthy > Degraded > Healthy
        if (response.Checks.Any(c => c.Status == "Unhealthy"))
            response.Status = "Unhealthy";
        else if (response.Checks.Any(c => c.Status == "Degraded"))
            response.Status = "Degraded";
        else
            response.Status = "Healthy";

        var statusCode = response.Status == "Unhealthy" ? 503 : 200;
        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Removes newline characters from a value before it is written to a log entry
    /// to prevent log-forging attacks.
    /// </summary>
    private static string? Sanitize(string? value) =>
        value?.Replace("\r", string.Empty, StringComparison.Ordinal)
              .Replace("\n", string.Empty, StringComparison.Ordinal);
}
