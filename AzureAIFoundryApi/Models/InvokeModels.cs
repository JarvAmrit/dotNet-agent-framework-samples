using System.ComponentModel.DataAnnotations;

namespace AzureAIFoundryApi.Models;

/// <summary>
/// Request model for invoking an agent with a user message.
/// </summary>
public class InvokeAgentRequest
{
    /// <summary>
    /// The message to send to the agent.
    /// </summary>
    [Required]
    public required string Message { get; set; }

    /// <summary>
    /// Optional existing thread ID to continue a conversation.
    /// If omitted, a new thread is created for this invocation.
    /// </summary>
    public string? ThreadId { get; set; }

    /// <summary>
    /// Maximum number of seconds to wait for the agent to respond.
    /// Defaults to 60 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// Response returned after invoking an agent.
/// </summary>
public class InvokeAgentResponse
{
    /// <summary>
    /// The thread ID used for this invocation. Use this in subsequent
    /// calls to continue the conversation.
    /// </summary>
    public string? ThreadId { get; set; }

    /// <summary>
    /// The ID of the run that was created.
    /// </summary>
    public string? RunId { get; set; }

    /// <summary>
    /// The final status of the run (e.g., "completed", "failed", "requires_action").
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// The messages in the thread after the run completes, ordered newest-first.
    /// </summary>
    public List<InvokeMessageResponse> Messages { get; set; } = new();
}

/// <summary>
/// A single message returned as part of an agent invocation.
/// </summary>
public class InvokeMessageResponse
{
    /// <summary>
    /// The unique message ID.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The role of the message sender: "user" or "assistant".
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string? Content { get; set; }
}
