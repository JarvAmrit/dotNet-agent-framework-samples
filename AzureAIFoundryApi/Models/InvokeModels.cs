using System.ComponentModel.DataAnnotations;

namespace AzureAIFoundryApi.Models;

public class InvokeAgentRequest
{
    /// <summary>The user message to send to the agent.</summary>
    [Required]
    public required string Message { get; set; }

    /// <summary>Optional existing thread ID to continue a conversation.</summary>
    public string? ThreadId { get; set; }

    /// <summary>Max seconds to wait for the run to complete. Defaults to 120.</summary>
    public int TimeoutSeconds { get; set; } = 120;
}

public class InvokeAgentResponse
{
    public string? ThreadId { get; set; }
    public string? RunId { get; set; }
    public string? Status { get; set; }
    public List<ThreadMessageResponse> Messages { get; set; } = new();
}

public class CreateRunRequest
{
    /// <summary>Name of the agent to run on this thread.</summary>
    [Required]
    public required string AgentName { get; set; }

    /// <summary>Optional instructions override for this run.</summary>
    public string? Instructions { get; set; }
}

public class RunResponse
{
    public string? RunId { get; set; }
    public string? ThreadId { get; set; }
    public string? AgentId { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? LastError { get; set; }
}

public class CreateThreadMessageRequest
{
    [Required]
    public required string Content { get; set; }

    /// <summary>Role of the message author. Defaults to "user".</summary>
    public string Role { get; set; } = "user";
}

public class ThreadResponse
{
    public string? ThreadId { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}

public class ThreadMessageResponse
{
    public string? MessageId { get; set; }
    public string? ThreadId { get; set; }
    public string? Role { get; set; }
    public string? Content { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}
