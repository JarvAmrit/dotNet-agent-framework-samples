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
/// Request model for creating a hosted agent from source code.
/// Deploys agents directly from zipped source code instead of container images.
/// </summary>
public class CreateHostedAgentFromSourceRequest
{
    /// <summary>
    /// The name for the agent.
    /// </summary>
    [Required]
    public required string AgentName { get; set; }

    /// <summary>
    /// Base64-encoded zip file containing the agent source code.
    /// </summary>
    [Required]
    public required string SourceCodeZipBase64 { get; set; }

    /// <summary>
    /// Runtime environment (e.g., "python_3_13", "python_3_14", "dotnet_10").
    /// </summary>
    [Required]
    public required string Runtime { get; set; }

    /// <summary>
    /// Entry point command or file for the agent (e.g., "main.py" for Python, "MyAgent.dll" for .NET).
    /// </summary>
    [Required]
    public required string EntryPoint { get; set; }

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
    /// Optional environment variables for the hosted agent.
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; set; }

    /// <summary>
    /// Optional description for the agent.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional build command to run during deployment (e.g., "pip install -r requirements.txt" or "dotnet restore").
    /// </summary>
    public string? BuildCommand { get; set; }
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
