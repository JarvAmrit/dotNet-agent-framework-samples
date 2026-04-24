using System.ComponentModel.DataAnnotations;

namespace AzureAIFoundryApi.Models;

/// <summary>
/// Request model for invoking an agent with a user message.
/// </summary>
public class InvokeAgentRequest
{
    /// <summary>
    /// The user message to send to the agent.
    /// </summary>
    [Required]
    public required string Message { get; set; }

    /// <summary>
    /// Pass the <c>responseId</c> returned from a previous invoke call to continue the
    /// same conversation. Omit or set to <c>null</c> to start a new conversation.
    /// </summary>
    public string? PreviousResponseId { get; set; }
}

/// <summary>
/// Response model returned after an agent invoke call completes.
/// </summary>
public class InvokeAgentResponse
{
    /// <summary>
    /// The unique ID of this response. Pass this as <c>previousResponseId</c> in the
    /// next call to continue the conversation.
    /// </summary>
    public string? ResponseId { get; set; }

    /// <summary>
    /// The ID of the previous response this continues, if any.
    /// </summary>
    public string? PreviousResponseId { get; set; }

    /// <summary>
    /// Status of the response (e.g., "Completed", "Failed", "Incomplete").
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// The assistant's text reply, extracted from the response output. This is
    /// <c>null</c> when the response contains no text output item.
    /// </summary>
    public string? AssistantMessage { get; set; }
}
