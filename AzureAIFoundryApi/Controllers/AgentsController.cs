#pragma warning disable OPENAI001  // OpenAI Assistants API is currently in experimental status
using System.ClientModel;
using Azure.AI.Projects.Agents;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Assistants;

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
        try
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
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = $"Agent not found." });
        }
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
        try
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
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = $"Agent version not found." });
        }
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
    [HttpPost("hosted")]
    public async Task<IActionResult> CreateHostedAgent([FromBody] CreateHostedAgentRequest request)
    {
        var client = _clientFactory.GetClient();
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
    /// Deletes a specific version of an agent.
    /// </summary>
    [HttpDelete("{agentName}/versions/{agentVersion}")]
    public async Task<IActionResult> DeleteAgentVersion(string agentName, string agentVersion)
    {
        var client = _clientFactory.GetClient();
        var agentAdmin = client.AgentAdministrationClient;

        await agentAdmin.DeleteAgentVersionAsync(agentName, agentVersion);

        _logger.LogInformation("Deleted agent version successfully");
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

        _logger.LogInformation("Deleted agent successfully");
        return NoContent();
    }

    /// <summary>
    /// Invokes an agent with a user message and returns the agent's response.
    /// Creates a new conversation thread (or reuses an existing one), sends the message,
    /// runs the agent, polls for completion, and returns the resulting messages.
    /// This endpoint is primarily intended for prompt (declarative) agents backed by
    /// the Azure AI Agents runtime.
    /// </summary>
    /// <param name="agentName">The name of the agent to invoke.</param>
    /// <param name="request">The invocation request containing the message and optional thread ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{agentName}/invoke")]
    public async Task<IActionResult> InvokeAgent(
        string agentName,
        [FromBody] InvokeAgentRequest request,
        CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();
        var agentAdmin = client.AgentAdministrationClient;

        // Retrieve the agent to verify it exists and obtain its runtime ID.
        ProjectsAgentRecord agent;
        try
        {
            var agentResult = await agentAdmin.GetAgentAsync(agentName);
            agent = agentResult.Value;
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = $"Agent '{agentName}' not found." });
        }

        if (string.IsNullOrWhiteSpace(agent.Id))
            return BadRequest(new { error = "Agent has no runtime ID and cannot be invoked." });

        // AssistantClient provides thread/message/run management via the OpenAI Assistants API.
        var assistantClient = client.ProjectOpenAIClient.GetAssistantClient();
        var timeoutSeconds = request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 60;

        // Create a new thread or reuse the provided one.
        string threadId;
        if (!string.IsNullOrWhiteSpace(request.ThreadId))
        {
            threadId = request.ThreadId;
        }
        else
        {
            var thread = (await assistantClient.CreateThreadAsync(
                options: null, cancellationToken: cancellationToken)).Value;
            threadId = thread.Id;
        }

        // Add the user message to the thread.
        await assistantClient.CreateMessageAsync(
            threadId,
            MessageRole.User,
            [MessageContent.FromText(request.Message)],
            options: null,
            cancellationToken: cancellationToken);

        // Create a run for the agent on this thread.
        var run = (await assistantClient.CreateRunAsync(
            threadId, agent.Id, options: null, cancellationToken: cancellationToken)).Value;

        // Poll until the run reaches a terminal state or the timeout is exceeded.
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (run.Status == RunStatus.Queued
            || run.Status == RunStatus.InProgress
            || run.Status == RunStatus.Cancelling)
        {
            if (DateTime.UtcNow >= deadline)
            {
                return StatusCode(408, new
                {
                    error = $"Agent invocation timed out after {timeoutSeconds} seconds.",
                    threadId,
                    runId = run.Id,
                    status = run.Status.ToString()
                });
            }

            await Task.Delay(1000, cancellationToken);
            run = (await assistantClient.GetRunAsync(
                threadId, run.Id, cancellationToken: cancellationToken)).Value;
        }

        _logger.LogInformation(
            "Agent '{AgentName}' run '{RunId}' finished with status '{Status}'",
            Sanitize(agentName), Sanitize(run.Id), run.Status);

        // Collect messages only when the run completed successfully.
        var messages = new List<InvokeMessageResponse>();
        if (run.Status == RunStatus.Completed)
        {
            await foreach (var message in assistantClient.GetMessagesAsync(
                threadId, options: null, cancellationToken: cancellationToken))
            {
                foreach (var content in message.Content)
                {
                    if (!string.IsNullOrEmpty(content.Text))
                    {
                        messages.Add(new InvokeMessageResponse
                        {
                            Id = message.Id,
                            Role = message.Role.ToString(),
                            Content = content.Text
                        });
                    }
                }
            }
        }

        return Ok(new InvokeAgentResponse
        {
            ThreadId = threadId,
            RunId = run.Id,
            Status = run.Status.ToString(),
            Messages = messages
        });
    }

    /// <summary>
    /// Checks the health of a specific agent by verifying it is reachable in Azure AI Foundry.
    /// Returns "Healthy" if the agent exists and can be retrieved, "Unhealthy" otherwise.
    /// </summary>
    /// <param name="agentName">The name of the agent to check.</param>
    [HttpGet("{agentName}/health")]
    public async Task<IActionResult> GetAgentHealth(string agentName)
    {
        var checkedAt = DateTime.UtcNow;
        try
        {
            var client = _clientFactory.GetClient();
            var agentAdmin = client.AgentAdministrationClient;

            var result = await agentAdmin.GetAgentAsync(agentName);
            var agent = result.Value;

            return Ok(new AgentHealthResponse
            {
                AgentName = agentName,
                Status = "Healthy",
                AgentId = agent.Id,
                CheckedAt = checkedAt
            });
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return Ok(new AgentHealthResponse
            {
                AgentName = agentName,
                Status = "Unhealthy",
                Error = "Agent not found in Azure AI Foundry.",
                CheckedAt = checkedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for agent '{AgentName}'", Sanitize(agentName));
            return Ok(new AgentHealthResponse
            {
                AgentName = agentName,
                Status = "Unhealthy",
                Error = ex.Message,
                CheckedAt = checkedAt
            });
        }
    }

    private static string Sanitize(string? value) =>
        (value ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal)
                               .Replace("\n", string.Empty, StringComparison.Ordinal);
}
