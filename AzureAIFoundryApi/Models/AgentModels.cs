using System.ComponentModel.DataAnnotations;

namespace AzureAIFoundryApi.Models;

/// <summary>
/// Request model for creating an agent in Azure AI Foundry.
/// </summary>
public class CreateAgentRequest
{
    /// <summary>
    /// The name for the agent (used as the agent identifier in Foundry).
    /// </summary>
    [Required]
    public required string AgentName { get; set; }

    /// <summary>
    /// The model deployment name to use for this agent (e.g., "gpt-4o", "gpt-4o-mini").
    /// </summary>
    [Required]
    public required string Model { get; set; }

    /// <summary>
    /// The system instructions for the agent.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Optional description for the agent.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request model for updating an existing agent version.
/// </summary>
public class UpdateAgentRequest
{
    /// <summary>
    /// The model deployment name to use for this agent.
    /// </summary>
    [Required]
    public required string Model { get; set; }

    /// <summary>
    /// The system instructions for the agent.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Optional description for the agent.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Response model representing an agent record from Azure AI Foundry.
/// </summary>
public class AgentResponse
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Request model for creating a hosted agent in Azure AI Foundry.
/// Hosted agents run as containerized services.
/// </summary>
public class CreateHostedAgentRequest
{
    /// <summary>
    /// The name for the agent.
    /// </summary>
    [Required]
    public required string AgentName { get; set; }

    /// <summary>
    /// The container image for the hosted agent.
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// CPU allocation (e.g., "1", "2"). Defaults to "1".
    /// </summary>
    public string? Cpu { get; set; }

    /// <summary>
    /// Memory allocation (e.g., "2Gi", "4Gi"). Defaults to "2Gi".
    /// </summary>
    public string? Memory { get; set; }

    /// <summary>
    /// Protocol versions supported by the hosted agent.
    /// </summary>
    [Required]
    public required List<ProtocolVersionInput> ProtocolVersions { get; set; }

    /// <summary>
    /// Optional environment variables for the hosted agent container.
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; set; }

    /// <summary>
    /// Optional description for the agent.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Input model for protocol version records.
/// </summary>
public class ProtocolVersionInput
{
    /// <summary>
    /// The protocol type (e.g., "A2A", "OpenAI").
    /// </summary>
    [Required]
    public required string Protocol { get; set; }

    /// <summary>
    /// The protocol version string.
    /// </summary>
    [Required]
    public required string Version { get; set; }
}

/// <summary>
/// Response model representing an agent version from Azure AI Foundry.
/// </summary>
public class AgentVersionResponse
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
}
