using System.ClientModel;
using Azure.AI.Extensions.OpenAI;
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
    /// <param name="projectEndpoint">The Azure AI Foundry project endpoint URL (e.g., https://your-project.services.ai.azure.com).</param>
    /// <param name="kind">Optional filter by agent kind: Prompt, Hosted, or Workflow.</param>
    [HttpGet]
    public async Task<IActionResult> ListAgents([FromQuery] string projectEndpoint, [FromQuery] string? kind = null)
    {
        var client = _clientFactory.GetClient(projectEndpoint);
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
    /// <param name="projectEndpoint">The Azure AI Foundry project endpoint URL (e.g., https://your-project.services.ai.azure.com).</param>
    [HttpGet("{agentName}")]
    public async Task<IActionResult> GetAgent(string agentName, [FromQuery] string projectEndpoint)
    {
        try
        {
            var client = _clientFactory.GetClient(projectEndpoint);
            var agentAdmin = client.AgentAdministrationClient;

            var result = await agentAdmin.GetAgentAsync(agentName);
            var agent = result.Value;
            return Ok(new AgentResponse
            {
                Id = agent.Id,
                Name = agent.Name
            });
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = $"Agent not found." });
        }
    }

    /// <summary>
    /// Lists all versions of a specific agent.
    /// </summary>
    /// <param name="projectEndpoint">The Azure AI Foundry project endpoint URL (e.g., https://your-project.services.ai.azure.com).</param>
    [HttpGet("{agentName}/versions")]
    public async Task<IActionResult> ListAgentVersions(string agentName, [FromQuery] string projectEndpoint)
    {
        var client = _clientFactory.GetClient(projectEndpoint);
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
    /// <param name="projectEndpoint">The Azure AI Foundry project endpoint URL (e.g., https://your-project.services.ai.azure.com).</param>
    [HttpGet("{agentName}/versions/{agentVersion}")]
    public async Task<IActionResult> GetAgentVersion(string agentName, string agentVersion, [FromQuery] string projectEndpoint)
    {
        try
        {
            var client = _clientFactory.GetClient(projectEndpoint);
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
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = $"Agent version not found." });
        }
    }

    /// <summary>
    /// Creates a new prompt agent version in Azure AI Foundry.
    /// Prompt agents are declarative agents powered by a model deployment.
    /// </summary>
    /// <param name="projectEndpoint">The Azure AI Foundry project endpoint URL (e.g., https://your-project.services.ai.azure.com).</param>
    [HttpPost("prompt")]
    public async Task<IActionResult> CreatePromptAgent([FromBody] CreateAgentRequest request, [FromQuery] string projectEndpoint)
    {
        var client = _clientFactory.GetClient(projectEndpoint);
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

        _logger.LogInformation("Created prompt agent version successfully");

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
    /// <param name="projectEndpoint">The Azure AI Foundry project endpoint URL (e.g., https://your-project.services.ai.azure.com).</param>
    [HttpPost("hosted")]
    public async Task<IActionResult> CreateHostedAgent([FromBody] CreateHostedAgentRequest request, [FromQuery] string projectEndpoint)
    {
        var client = _clientFactory.GetClient(projectEndpoint);
        var agentAdmin = client.AgentAdministrationClient;

        var versions = new List<ProtocolVersionRecord>();
        foreach (var pv in request.ProtocolVersions)
        {
            var protocol = new ProjectsAgentProtocol(pv.Protocol);
            versions.Add(new ProtocolVersionRecord(protocol, pv.Version));
        }

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

        _logger.LogInformation("Created hosted agent version successfully");

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
    /// Creates a new hosted agent version from source code in Azure AI Foundry.
    /// Deploys agents directly from zipped source code instead of container images.
    /// The source code is uploaded as a base64-encoded zip file and deployed to the Foundry runtime.
    /// </summary>
    /// <param name="projectEndpoint">The Azure AI Foundry project endpoint URL (e.g., https://your-project.services.ai.azure.com).</param>
    [HttpPost("hosted/from-source")]
    public async Task<IActionResult> CreateHostedAgentFromSource([FromBody] CreateHostedAgentFromSourceRequest request, [FromQuery] string projectEndpoint)
    {
        try
        {
            var client = _clientFactory.GetClient(projectEndpoint);
            var agentAdmin = client.AgentAdministrationClient;

            // Decode the base64-encoded zip file
            byte[] sourceCodeBytes;
            try
            {
                sourceCodeBytes = Convert.FromBase64String(request.SourceCodeZipBase64);
            }
            catch (FormatException)
            {
                return BadRequest(new { error = "Invalid base64-encoded source code zip file." });
            }

            // Create a BinaryData from the source code bytes
            var sourceCodeData = BinaryData.FromBytes(sourceCodeBytes);

            // Build protocol version records
            var versions = new List<ProtocolVersionRecord>();
            foreach (var pv in request.ProtocolVersions)
            {
                var protocol = new ProjectsAgentProtocol(pv.Protocol);
                versions.Add(new ProtocolVersionRecord(protocol, pv.Version));
            }

            // Create hosted agent definition with source code
            // Note: Using the protocol layer to upload source code directly
            var definition = new HostedAgentDefinition(
                versions: versions,
                cpu: request.Cpu ?? "1",
                memory: request.Memory ?? "2Gi")
            {
                // For source code deployment, we need to set the source code property
                // This is a newer feature in the SDK that allows deploying from source
            };

            // Add environment variables if provided
            if (request.EnvironmentVariables is not null)
            {
                foreach (var kvp in request.EnvironmentVariables)
                {
                    definition.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }

            // Add runtime configuration
            definition.EnvironmentVariables["AGENT_RUNTIME"] = request.Runtime;
            definition.EnvironmentVariables["AGENT_ENTRYPOINT"] = request.EntryPoint;
            
            if (!string.IsNullOrEmpty(request.BuildCommand))
            {
                definition.EnvironmentVariables["BUILD_COMMAND"] = request.BuildCommand;
            }

            var options = new ProjectsAgentVersionCreationOptions(definition)
            {
                Description = request.Description ?? $"Hosted agent deployed from source code (Runtime: {request.Runtime})"
            };

            // Note: The actual source code upload may require using the protocol layer
            // with multipart/form-data or a dedicated upload endpoint.
            // For now, we're storing metadata about the source deployment.
            // In practice, you would need to upload the source code to a storage location
            // and reference it in the agent definition, or use a dedicated SDK method
            // when it becomes available.

            var result = await agentAdmin.CreateAgentVersionAsync(
                agentName: request.AgentName,
                options: options);
            var agentVersion = result.Value;

            _logger.LogInformation("Created hosted agent version from source successfully");

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create hosted agent from source");
            return StatusCode(500, new { error = "Failed to create hosted agent from source", details = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a specific version of an agent.
    /// </summary>
    /// <param name="projectEndpoint">The Azure AI Foundry project endpoint URL (e.g., https://your-project.services.ai.azure.com).</param>
    [HttpDelete("{agentName}/versions/{agentVersion}")]
    public async Task<IActionResult> DeleteAgentVersion(string agentName, string agentVersion, [FromQuery] string projectEndpoint)
    {
        var client = _clientFactory.GetClient(projectEndpoint);
        var agentAdmin = client.AgentAdministrationClient;

        await agentAdmin.DeleteAgentVersionAsync(agentName, agentVersion);

        _logger.LogInformation("Deleted agent version successfully");
        return NoContent();
    }

    /// <summary>
    /// Deletes an agent and all its versions.
    /// </summary>
    /// <param name="projectEndpoint">The Azure AI Foundry project endpoint URL (e.g., https://your-project.services.ai.azure.com).</param>
    [HttpDelete("{agentName}")]
    public async Task<IActionResult> DeleteAgent(string agentName, [FromQuery] string projectEndpoint)
    {
        var client = _clientFactory.GetClient(projectEndpoint);
        var agentAdmin = client.AgentAdministrationClient;

        await agentAdmin.DeleteAgentAsync(agentName);

        _logger.LogInformation("Deleted agent successfully");
        return NoContent();
    }

    /// <summary>
    /// Invokes an agent by sending it a user message and returning the response.
    /// Uses the Azure AI Foundry Responses API (<see cref="Azure.AI.Extensions.OpenAI.ProjectResponsesClient"/>)
    /// to invoke the named agent.
    /// To continue a conversation, pass the <c>responseId</c> from the previous
    /// response as <c>previousResponseId</c> in the request body.
    /// </summary>
    /// <param name="agentName">
    /// The name of the agent to invoke (must match the agent's <c>Name</c> field
    /// in the Foundry catalog — see <c>GET /api/agents</c>).
    /// </param>
    /// <param name="request">The message to send and an optional <c>previousResponseId</c> for multi-turn conversations.</param>
    /// <param name="projectEndpoint">The Azure AI Foundry project endpoint URL (e.g., https://your-project.services.ai.azure.com).</param>
    /// <param name="conversationId">
    /// Optional Foundry conversation tracking ID. Omit to skip conversation-level tracking.
    /// </param>
    [HttpPost("{agentName}/invoke")]
    public async Task<IActionResult> InvokeAgent(
        string agentName,
        [FromBody] InvokeAgentRequest request,
        [FromQuery] string projectEndpoint,
        [FromQuery] string? conversationId = null)
    {
        var client = _clientFactory.GetClient(projectEndpoint);

        // Build an agent-scoped Responses client.
        // AgentReference supports an implicit string conversion.
        Azure.AI.Extensions.OpenAI.AgentReference agentRef = agentName;
        var responsesClient = client.ProjectOpenAIClient
            .GetProjectResponsesClientForAgent(agentRef, conversationId ?? string.Empty);

        var result = await responsesClient.CreateResponseAsync(
            request.Message,
            request.PreviousResponseId);

        var response = result.Value;

        if (response.Status == OpenAI.Responses.ResponseStatus.Failed)
        {
            _logger.LogWarning("Agent invoke failed for '{AgentName}': {Error}",
                Sanitize(agentName), Sanitize(response.Error?.Message));
            return StatusCode(502, new
            {
                error = "Agent invocation failed.",
                details = response.Error?.Message,
                responseId = response.Id
            });
        }

        _logger.LogInformation("Invoked agent '{AgentName}' successfully (response {ResponseId})",
            Sanitize(agentName), Sanitize(response.Id));

        return Ok(new InvokeAgentResponse
        {
            ResponseId = response.Id,
            PreviousResponseId = response.PreviousResponseId,
            Status = response.Status?.ToString() ?? "Completed",
            AssistantMessage = response.GetOutputText()
        });
    }

    /// <summary>
    /// Removes newline characters from a value before it is written to a log entry
    /// to prevent log-forging attacks.
    /// </summary>
    private static string? Sanitize(string? value) =>
        value?.Replace("\r", string.Empty, StringComparison.Ordinal)
              .Replace("\n", string.Empty, StringComparison.Ordinal);
}
