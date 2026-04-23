namespace AzureAIFoundryApi.Models;

/// <summary>
/// Response model for the health status of a single agent.
/// </summary>
public class AgentHealthResponse
{
    /// <summary>
    /// The name of the agent that was checked.
    /// </summary>
    public string? AgentName { get; set; }

    /// <summary>
    /// The health status: "Healthy" or "Unhealthy".
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// The agent version ID returned by Azure AI Foundry, when the agent is reachable.
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// Error details when the agent is unhealthy.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The UTC timestamp at which the health check was performed.
    /// </summary>
    public DateTime CheckedAt { get; set; }
}
