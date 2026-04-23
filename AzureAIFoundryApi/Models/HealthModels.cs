namespace AzureAIFoundryApi.Models;

/// <summary>
/// Overall health response returned by the health endpoints.
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Aggregate health status: "Healthy", "Degraded", or "Unhealthy".
    /// </summary>
    public string Status { get; set; } = "Healthy";

    /// <summary>
    /// UTC timestamp when the health check was performed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Individual check results that contributed to the aggregate status.
    /// </summary>
    public List<HealthCheckResult> Checks { get; set; } = [];
}

/// <summary>
/// Health response model that additionally carries per-agent information.
/// </summary>
public class AgentHealthResponse : HealthResponse
{
    /// <summary>
    /// The name of the agent that was checked.
    /// </summary>
    public string? AgentName { get; set; }

    /// <summary>
    /// The catalog ID of the agent, if it was found.
    /// </summary>
    public string? AgentId { get; set; }
}

/// <summary>
/// Result of a single named health check.
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// A short descriptive name for this check (e.g., "FoundryConnectivity").
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Status of this individual check: "Healthy", "Degraded", or "Unhealthy".
    /// </summary>
    public string Status { get; set; } = "Healthy";

    /// <summary>
    /// A human-readable description of what was checked and the outcome.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Error message populated when the check is not "Healthy".
    /// </summary>
    public string? Error { get; set; }
}
