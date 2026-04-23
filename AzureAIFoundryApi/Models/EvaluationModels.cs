using System.ComponentModel.DataAnnotations;

namespace AzureAIFoundryApi.Models;

/// <summary>
/// Response model for an evaluator version in Azure AI Foundry.
/// </summary>
public class EvaluatorVersionResponse
{
    /// <summary>The unique ID of this evaluator version.</summary>
    public string? Id { get; set; }

    /// <summary>The evaluator name (registry key).</summary>
    public string? Name { get; set; }

    /// <summary>Human-readable display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Evaluator type: "BuiltIn" or "Custom".</summary>
    public string? Type { get; set; }

    /// <summary>ISO 8601 creation timestamp.</summary>
    public string? CreatedAt { get; set; }

    /// <summary>ISO 8601 last-modified timestamp.</summary>
    public string? ModifiedAt { get; set; }
}

/// <summary>
/// Response model for a continuous evaluation rule.
/// </summary>
public class EvaluationRuleResponse
{
    /// <summary>The rule ID.</summary>
    public string? Id { get; set; }

    /// <summary>Human-readable display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Description of what the rule does.</summary>
    public string? Description { get; set; }

    /// <summary>The event type that triggers the rule (e.g. "OnMessage").</summary>
    public string? EventType { get; set; }

    /// <summary>Whether the rule is currently active.</summary>
    public bool Enabled { get; set; }
}

/// <summary>
/// Request model for creating or updating a continuous evaluation rule.
/// </summary>
public class CreateEvaluationRuleRequest
{
    /// <summary>
    /// Human-readable display name for the rule.
    /// </summary>
    [Required]
    public required string DisplayName { get; set; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The name or registry ID of the evaluator to run (e.g. "coherence").
    /// </summary>
    [Required]
    public required string EvaluatorName { get; set; }

    /// <summary>
    /// The event type that triggers evaluation: "ResponseCompleted" or "Manual".
    /// </summary>
    [Required]
    public required string EventType { get; set; }

    /// <summary>
    /// Whether the rule should be active immediately.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
