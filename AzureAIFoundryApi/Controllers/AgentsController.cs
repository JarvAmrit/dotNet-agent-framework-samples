using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryApi.Controllers;

/// <summary>
/// Controller for managing agents (prompt and hosted) in Azure AI Foundry.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly AIProjectClientFactory _clientFactory;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(AIProjectClientFactory clientFactory, ILogger<AgentsController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Lists all agents in the Azure AI Foundry project.
    /// </summary>
    /// <param name="kind">Optional filter by agent kind: Prompt, Hosted, or Workflow.</param>
    [HttpGet]
    public async Task<IActionResult> ListAgents([FromQuery] string? kind = null)
    {
        var client = _clientFactory.GetClient();
        var agentAdmin = client.AgentAdministrationClient;

        ProjectsAgentKind? agentKind = kind?.ToLowerInvariant() switch
        {
            "prompt" => ProjectsAgentKind.Prompt,
            "hosted" => ProjectsAgentKind.Hosted,
            "workflow" => ProjectsAgentKind.Workflow,
            _ => null
        };

        var agents = new List<AgentResponse>();
        await foreach (var agent in agentAdmin.GetAgentsAsync(agentKind))
        {
            agents.Add(new AgentResponse
            {
                Id = agent.Id,
                Name = agent.Name
            });
        }

        return Ok(agents);
    }

    /// <summary>
    /// Gets a specific agent by name.
    /// </summary>
    [HttpGet("{agentName}")]
    public async Task<IActionResult> GetAgent(string agentName)
    {
        var client = _clientFactory.GetClient();
        var agentAdmin = client.AgentAdministrationClient;

        var result = await agentAdmin.GetAgentAsync(agentName);
        var agent = result.Value;
        return Ok(new AgentResponse
        {
            Id = agent.Id,
            Name = agent.Name
        });
    }

    /// <summary>
    /// Lists all versions of a specific agent.
    /// </summary>
    [HttpGet("{agentName}/versions")]
    public async Task<IActionResult> ListAgentVersions(string agentName)
    {
        var client = _clientFactory.GetClient();
        var agentAdmin = client.AgentAdministrationClient;

        var versions = new List<AgentVersionResponse>();
        await foreach (var version in agentAdmin.GetAgentVersionsAsync(agentName))
        {
            versions.Add(new AgentVersionResponse
            {
                Id = version.Id,
                Name = version.Name,
                Version = version.Version
            });
        }

        return Ok(versions);
    }

    /// <summary>
    /// Gets a specific version of an agent.
    /// </summary>
    [HttpGet("{agentName}/versions/{agentVersion}")]
    public async Task<IActionResult> GetAgentVersion(string agentName, string agentVersion)
    {
        var client = _clientFactory.GetClient();
        var agentAdmin = client.AgentAdministrationClient;

        var result = await agentAdmin.GetAgentVersionAsync(agentName, agentVersion);
        var version = result.Value;
        return Ok(new AgentVersionResponse
        {
            Id = version.Id,
            Name = version.Name,
            Version = version.Version
        });
    }

    /// <summary>
    /// Creates a new prompt agent version in Azure AI Foundry.
    /// Prompt agents are declarative agents powered by a model deployment.
    /// </summary>
    [HttpPost("prompt")]
    public async Task<IActionResult> CreatePromptAgent([FromBody] CreateAgentRequest request)
    {
        var client = _clientFactory.GetClient();
        var agentAdmin = client.AgentAdministrationClient;

        var definition = new DeclarativeAgentDefinition(model: request.Model)
        {
            Instructions = request.Instructions
        };

        var options = new ProjectsAgentVersionCreationOptions(definition)
        {
            Description = request.Description
        };

        var result = await agentAdmin.CreateAgentVersionAsync(
            agentName: request.AgentName,
            options: options);
        var agentVersion = result.Value;

        _logger.LogInformation(
            "Created prompt agent '{AgentName}' version '{Version}'",
            agentVersion.Name,
            agentVersion.Version);

        return CreatedAtAction(
            nameof(GetAgentVersion),
            new { agentName = agentVersion.Name, agentVersion = agentVersion.Version },
            new AgentVersionResponse
            {
                Id = agentVersion.Id,
                Name = agentVersion.Name,
                Version = agentVersion.Version
            });
    }

    /// <summary>
    /// Creates a new hosted agent version in Azure AI Foundry.
    /// Hosted agents run as containerized services within Foundry.
    /// </summary>
    [HttpPost("hosted")]
    public async Task<IActionResult> CreateHostedAgent([FromBody] CreateHostedAgentRequest request)
    {
        var client = _clientFactory.GetClient();
        var agentAdmin = client.AgentAdministrationClient;

        var versions = request.ProtocolVersions
            .Select(pv => new ProtocolVersionRecord(
                Enum.Parse<ProjectsAgentProtocol>(pv.Protocol, ignoreCase: true),
                pv.Version))
            .ToList();

        var definition = new HostedAgentDefinition(
            versions: versions,
            cpu: request.Cpu ?? "1",
            memory: request.Memory ?? "2Gi")
        {
            Image = request.Image
        };

        if (request.EnvironmentVariables is not null)
        {
            foreach (var kvp in request.EnvironmentVariables)
            {
                definition.EnvironmentVariables[kvp.Key] = kvp.Value;
            }
        }

        var options = new ProjectsAgentVersionCreationOptions(definition)
        {
            Description = request.Description
        };

        var result = await agentAdmin.CreateAgentVersionAsync(
            agentName: request.AgentName,
            options: options);
        var agentVersion = result.Value;

        _logger.LogInformation(
            "Created hosted agent '{AgentName}' version '{Version}'",
            agentVersion.Name,
            agentVersion.Version);

        return CreatedAtAction(
            nameof(GetAgentVersion),
            new { agentName = agentVersion.Name, agentVersion = agentVersion.Version },
            new AgentVersionResponse
            {
                Id = agentVersion.Id,
                Name = agentVersion.Name,
                Version = agentVersion.Version
            });
    }

    /// <summary>
    /// Deletes a specific version of an agent.
    /// </summary>
    [HttpDelete("{agentName}/versions/{agentVersion}")]
    public async Task<IActionResult> DeleteAgentVersion(string agentName, string agentVersion)
    {
        var client = _clientFactory.GetClient();
        var agentAdmin = client.AgentAdministrationClient;

        await agentAdmin.DeleteAgentVersionAsync(agentName, agentVersion);

        _logger.LogInformation("Deleted agent '{AgentName}' version '{Version}'", agentName, agentVersion);
        return NoContent();
    }

    /// <summary>
    /// Deletes an agent and all its versions.
    /// </summary>
    [HttpDelete("{agentName}")]
    public async Task<IActionResult> DeleteAgent(string agentName)
    {
        var client = _clientFactory.GetClient();
        var agentAdmin = client.AgentAdministrationClient;

        await agentAdmin.DeleteAgentAsync(agentName);

        _logger.LogInformation("Deleted agent '{AgentName}'", agentName);
        return NoContent();
    }
}
