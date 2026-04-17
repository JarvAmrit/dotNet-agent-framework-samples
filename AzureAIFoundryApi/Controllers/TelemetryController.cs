using Azure.AI.Projects;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryApi.Controllers;

/// <summary>
/// Controller for retrieving telemetry configuration from Azure AI Foundry.
/// Provides the Application Insights connection string for the linked workspace,
/// enabling distributed tracing and observability in agent applications.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly AIProjectClientFactory _clientFactory;
    private readonly ILogger<TelemetryController> _logger;

    public TelemetryController(AIProjectClientFactory clientFactory, ILogger<TelemetryController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the Application Insights connection string for the Azure AI Foundry project.
    /// Use this connection string to configure OpenTelemetry or Application Insights SDKs
    /// to send traces and logs to the Foundry-linked Application Insights workspace.
    /// </summary>
    [HttpGet("app-insights")]
    public async Task<IActionResult> GetAppInsightsConnectionString()
    {
        var client = _clientFactory.GetClient();
        var telemetry = client.Telemetry;

        var connectionString = await telemetry.GetApplicationInsightsConnectionStringAsync();

        _logger.LogInformation("Retrieved Application Insights connection string");

        return Ok(new AppInsightsConnectionStringResponse
        {
            ConnectionString = connectionString
        });
    }
}
