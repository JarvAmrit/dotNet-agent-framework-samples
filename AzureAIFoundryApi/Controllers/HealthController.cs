using System.Diagnostics;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryApi.Controllers;

/// <summary>
/// Health check endpoints for monitoring connectivity to each Azure AI Foundry subsystem.
/// Returns HTTP 200 when healthy, HTTP 503 when degraded/unhealthy.
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
    /// Aggregated health check across all Azure AI Foundry subsystems.
    /// Returns Healthy (200) or Degraded (503) with per-subsystem details.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var checks = await Task.WhenAll(
            ProbeAsync("agents", CheckAgentsAsync),
            ProbeAsync("connections", CheckConnectionsAsync),
            ProbeAsync("deployments", CheckDeploymentsAsync),
            ProbeAsync("indexes", CheckIndexesAsync),
            ProbeAsync("datasets", CheckDatasetsAsync),
            ProbeAsync("telemetry", CheckTelemetryAsync)
        );

        var subsystems = checks.ToDictionary(kv => kv.Key, kv => kv.Value);
        var allHealthy = subsystems.Values.All(s => s.Status == "Healthy");

        var response = new HealthResponse
        {
            Status = allHealthy ? "Healthy" : "Degraded",
            Timestamp = DateTimeOffset.UtcNow,
            Subsystems = subsystems
        };

        return allHealthy ? Ok(response) : StatusCode(503, response);
    }

    /// <summary>
    /// Health check for the Agents Administration subsystem.
    /// Verifies connectivity to the agent catalog endpoint.
    /// </summary>
    [HttpGet("agents")]
    public async Task<IActionResult> GetAgentsHealth()
    {
        var health = await CheckAgentsAsync();
        return health.Status == "Healthy" ? Ok(health) : StatusCode(503, health);
    }

    /// <summary>
    /// Health check for the Connections subsystem.
    /// Verifies connectivity to the project connections endpoint.
    /// </summary>
    [HttpGet("connections")]
    public async Task<IActionResult> GetConnectionsHealth()
    {
        var health = await CheckConnectionsAsync();
        return health.Status == "Healthy" ? Ok(health) : StatusCode(503, health);
    }

    /// <summary>
    /// Health check for the Deployments subsystem.
    /// Verifies connectivity to the model deployments endpoint.
    /// </summary>
    [HttpGet("deployments")]
    public async Task<IActionResult> GetDeploymentsHealth()
    {
        var health = await CheckDeploymentsAsync();
        return health.Status == "Healthy" ? Ok(health) : StatusCode(503, health);
    }

    /// <summary>
    /// Health check for the Indexes subsystem.
    /// Verifies connectivity to the vector search indexes endpoint.
    /// </summary>
    [HttpGet("indexes")]
    public async Task<IActionResult> GetIndexesHealth()
    {
        var health = await CheckIndexesAsync();
        return health.Status == "Healthy" ? Ok(health) : StatusCode(503, health);
    }

    /// <summary>
    /// Health check for the Datasets subsystem.
    /// Verifies connectivity to the datasets endpoint.
    /// </summary>
    [HttpGet("datasets")]
    public async Task<IActionResult> GetDatasetsHealth()
    {
        var health = await CheckDatasetsAsync();
        return health.Status == "Healthy" ? Ok(health) : StatusCode(503, health);
    }

    /// <summary>
    /// Health check for the Telemetry subsystem.
    /// Verifies that the Application Insights connection string can be retrieved.
    /// </summary>
    [HttpGet("telemetry")]
    public async Task<IActionResult> GetTelemetryHealth()
    {
        var health = await CheckTelemetryAsync();
        return health.Status == "Healthy" ? Ok(health) : StatusCode(503, health);
    }

    private static async Task<KeyValuePair<string, SubsystemHealth>> ProbeAsync(
        string name, Func<Task<SubsystemHealth>> check)
    {
        var result = await check();
        return new KeyValuePair<string, SubsystemHealth>(name, result);
    }

    private async Task<SubsystemHealth> CheckAgentsAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = _clientFactory.GetClient();
            await foreach (var _ in client.AgentAdministrationClient.GetAgentsAsync())
            {
                break;
            }
            sw.Stop();
            return new SubsystemHealth { Status = "Healthy", LatencyMs = sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Agents subsystem health check failed");
            return new SubsystemHealth { Status = "Unhealthy", Message = ex.Message, LatencyMs = sw.ElapsedMilliseconds };
        }
    }

    private async Task<SubsystemHealth> CheckConnectionsAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = _clientFactory.GetClient();
            await foreach (var _ in client.Connections.GetConnectionsAsync())
            {
                break;
            }
            sw.Stop();
            return new SubsystemHealth { Status = "Healthy", LatencyMs = sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Connections subsystem health check failed");
            return new SubsystemHealth { Status = "Unhealthy", Message = ex.Message, LatencyMs = sw.ElapsedMilliseconds };
        }
    }

    private async Task<SubsystemHealth> CheckDeploymentsAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = _clientFactory.GetClient();
            await Task.Run(() =>
            {
                foreach (var _ in client.Deployments.GetDeployments())
                {
                    break;
                }
            });
            sw.Stop();
            return new SubsystemHealth { Status = "Healthy", LatencyMs = sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Deployments subsystem health check failed");
            return new SubsystemHealth { Status = "Unhealthy", Message = ex.Message, LatencyMs = sw.ElapsedMilliseconds };
        }
    }

    private async Task<SubsystemHealth> CheckIndexesAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = _clientFactory.GetClient();
            await foreach (var _ in client.Indexes.GetIndexesAsync())
            {
                break;
            }
            sw.Stop();
            return new SubsystemHealth { Status = "Healthy", LatencyMs = sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Indexes subsystem health check failed");
            return new SubsystemHealth { Status = "Unhealthy", Message = ex.Message, LatencyMs = sw.ElapsedMilliseconds };
        }
    }

    private async Task<SubsystemHealth> CheckDatasetsAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = _clientFactory.GetClient();
            await foreach (var _ in client.Datasets.GetDatasetsAsync())
            {
                break;
            }
            sw.Stop();
            return new SubsystemHealth { Status = "Healthy", LatencyMs = sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Datasets subsystem health check failed");
            return new SubsystemHealth { Status = "Unhealthy", Message = ex.Message, LatencyMs = sw.ElapsedMilliseconds };
        }
    }

    private async Task<SubsystemHealth> CheckTelemetryAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = _clientFactory.GetClient();
            await client.Telemetry.GetApplicationInsightsConnectionStringAsync();
            sw.Stop();
            return new SubsystemHealth { Status = "Healthy", LatencyMs = sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Telemetry subsystem health check failed");
            return new SubsystemHealth { Status = "Unhealthy", Message = ex.Message, LatencyMs = sw.ElapsedMilliseconds };
        }
    }
}
