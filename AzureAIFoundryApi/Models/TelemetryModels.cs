namespace AzureAIFoundryApi.Models;

/// <summary>
/// Response model for the Azure Application Insights telemetry connection details.
/// </summary>
public class AppInsightsConnectionStringResponse
{
    /// <summary>
    /// The Application Insights connection string for the Foundry project's linked workspace.
    /// Use this value to configure OpenTelemetry or Application Insights SDKs for tracing.
    /// </summary>
    public string? ConnectionString { get; set; }
}
