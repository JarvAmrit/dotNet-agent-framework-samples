using System.ClientModel;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryApi.Controllers;

/// <summary>
/// Controller for invoking agents and managing conversation threads, runs, and messages
/// via the Azure AI Projects Agents API (OpenAI Assistants-compatible interface).
/// </summary>
[ApiController]
[Route("api/agent-threads")]
public class AgentThreadsController : ControllerBase
{
    private readonly AIProjectClientFactory _clientFactory;
    private readonly ILogger<AgentThreadsController> _logger;

    public AgentThreadsController(AIProjectClientFactory clientFactory, ILogger<AgentThreadsController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>
    /// One-shot invoke: creates a thread, sends a message, runs the named agent, polls until
    /// completion, and returns all messages. Use ThreadId in the request to resume an existing
    /// conversation. Times out after TimeoutSeconds (default 120).
    /// </summary>
    /// <param name="agentName">Name of the agent to invoke (must exist in the project).</param>
    [HttpPost("~/api/agents/{agentName}/invoke")]
    public async Task<IActionResult> InvokeAgent(
        string agentName,
        [FromBody] InvokeAgentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.GetClient();
            var agentsClient = client.Agents;

            // Resolve the agent ID from the catalog
            ProjectsAgentRecord agent;
            try
            {
                var agentResult = await client.AgentAdministrationClient.GetAgentAsync(agentName);
                agent = agentResult.Value;
            }
            catch (ClientResultException ex) when (ex.Status == 404)
            {
                return NotFound(new { error = $"Agent '{agentName}' not found." });
            }

            // Create a new thread or reuse an existing one
            string threadId;
            if (!string.IsNullOrWhiteSpace(request.ThreadId))
            {
                threadId = request.ThreadId;
            }
            else
            {
                var thread = await agentsClient.CreateThreadAsync();
                threadId = thread.Value.Id;
            }

            // Add the user message
            await agentsClient.CreateMessageAsync(threadId, MessageRole.User, request.Message);

            // Start the run
            var run = await agentsClient.CreateRunAsync(threadId, agent.Id);

            // Poll until terminal state or timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));

            while (run.Value.Status == RunStatus.Queued ||
                   run.Value.Status == RunStatus.InProgress ||
                   run.Value.Status == RunStatus.Cancelling)
            {
                await Task.Delay(1000, cts.Token);
                run = await agentsClient.GetRunAsync(threadId, run.Value.Id, cts.Token);
            }

            var runStatus = run.Value.Status.ToString();
            var messages = new List<ThreadMessageResponse>();

            if (run.Value.Status == RunStatus.Completed)
            {
                await foreach (var msg in agentsClient.GetMessagesAsync(threadId, cancellationToken: cancellationToken))
                {
                    messages.Add(MapMessage(msg));
                }
            }

            _logger.LogInformation("Agent '{AgentName}' invoke completed with status {Status}", agentName, runStatus);

            return Ok(new InvokeAgentResponse
            {
                ThreadId = threadId,
                RunId = run.Value.Id,
                Status = runStatus,
                Messages = messages
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new { error = "Request timed out waiting for the agent run to complete." });
        }
    }

    /// <summary>
    /// Creates a new empty conversation thread.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateThread(CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();
        var thread = await client.Agents.CreateThreadAsync(cancellationToken);

        _logger.LogInformation("Created thread {ThreadId}", thread.Value.Id);

        return Ok(new ThreadResponse
        {
            ThreadId = thread.Value.Id,
            CreatedAt = thread.Value.CreatedAt
        });
    }

    /// <summary>
    /// Deletes a thread and all its messages.
    /// </summary>
    [HttpDelete("{threadId}")]
    public async Task<IActionResult> DeleteThread(string threadId, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.GetClient();
            await client.Agents.DeleteThreadAsync(threadId, cancellationToken);
            _logger.LogInformation("Deleted thread {ThreadId}", threadId);
            return NoContent();
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Thread not found." });
        }
    }

    /// <summary>
    /// Lists all messages in a thread, ordered newest-first.
    /// </summary>
    [HttpGet("{threadId}/messages")]
    public async Task<IActionResult> ListMessages(string threadId, CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();
        var messages = new List<ThreadMessageResponse>();

        await foreach (var msg in client.Agents.GetMessagesAsync(threadId, cancellationToken: cancellationToken))
        {
            messages.Add(MapMessage(msg));
        }

        return Ok(messages);
    }

    /// <summary>
    /// Adds a message to an existing thread.
    /// </summary>
    [HttpPost("{threadId}/messages")]
    public async Task<IActionResult> CreateMessage(
        string threadId,
        [FromBody] CreateThreadMessageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.GetClient();
            var role = request.Role.ToLowerInvariant() == "assistant" ? MessageRole.Agent : MessageRole.User;
            var msg = await client.Agents.CreateMessageAsync(threadId, role, request.Content, cancellationToken: cancellationToken);

            _logger.LogInformation("Added message to thread {ThreadId}", threadId);

            return Ok(MapMessage(msg.Value));
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Thread not found." });
        }
    }

    /// <summary>
    /// Starts a run on a thread using a named agent. Poll GET /runs/{runId} for status.
    /// </summary>
    [HttpPost("{threadId}/runs")]
    public async Task<IActionResult> CreateRun(
        string threadId,
        [FromBody] CreateRunRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.GetClient();

            ProjectsAgentRecord agent;
            try
            {
                var agentResult = await client.AgentAdministrationClient.GetAgentAsync(request.AgentName);
                agent = agentResult.Value;
            }
            catch (ClientResultException ex) when (ex.Status == 404)
            {
                return NotFound(new { error = $"Agent '{request.AgentName}' not found." });
            }

            var options = new CreateRunOptions(agent.Id);
            if (!string.IsNullOrWhiteSpace(request.Instructions))
                options.AdditionalInstructions = request.Instructions;

            var run = await client.Agents.CreateRunAsync(threadId, options, cancellationToken);

            _logger.LogInformation("Started run {RunId} on thread {ThreadId}", run.Value.Id, threadId);

            return Ok(MapRun(run.Value, threadId));
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Thread not found." });
        }
    }

    /// <summary>
    /// Gets the status of a run. Poll this endpoint until Status is Completed, Failed,
    /// Cancelled, or Expired.
    /// </summary>
    [HttpGet("{threadId}/runs/{runId}")]
    public async Task<IActionResult> GetRun(
        string threadId,
        string runId,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.GetClient();
            var run = await client.Agents.GetRunAsync(threadId, runId, cancellationToken);
            return Ok(MapRun(run.Value, threadId));
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Run or thread not found." });
        }
    }

    /// <summary>
    /// Lists all runs on a thread.
    /// </summary>
    [HttpGet("{threadId}/runs")]
    public async Task<IActionResult> ListRuns(string threadId, CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();
        var runs = new List<RunResponse>();

        await foreach (var run in client.Agents.GetRunsAsync(threadId, cancellationToken: cancellationToken))
        {
            runs.Add(MapRun(run, threadId));
        }

        return Ok(runs);
    }

    /// <summary>
    /// Cancels an in-progress run.
    /// </summary>
    [HttpPost("{threadId}/runs/{runId}/cancel")]
    public async Task<IActionResult> CancelRun(
        string threadId,
        string runId,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.GetClient();
            var run = await client.Agents.CancelRunAsync(threadId, runId, cancellationToken);
            _logger.LogInformation("Cancelled run {RunId} on thread {ThreadId}", runId, threadId);
            return Ok(MapRun(run.Value, threadId));
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Run or thread not found." });
        }
    }

    private static ThreadMessageResponse MapMessage(ThreadMessage msg)
    {
        var text = string.Join("\n", msg.Content
            .OfType<MessageTextContent>()
            .Select(c => c.Text));

        return new ThreadMessageResponse
        {
            MessageId = msg.Id,
            ThreadId = msg.ThreadId,
            Role = msg.Role.ToString(),
            Content = text,
            CreatedAt = msg.CreatedAt
        };
    }

    private static RunResponse MapRun(ThreadRun run, string threadId) =>
        new()
        {
            RunId = run.Id,
            ThreadId = threadId,
            AgentId = run.AssistantId,
            Status = run.Status.ToString(),
            CreatedAt = run.CreatedAt,
            CompletedAt = run.CompletedAt,
            LastError = run.LastError?.Message
        };
}
