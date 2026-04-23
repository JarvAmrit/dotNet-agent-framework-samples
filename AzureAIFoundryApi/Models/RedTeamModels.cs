using System.ComponentModel.DataAnnotations;

namespace AzureAIFoundryApi.Models;

/// <summary>
/// Response model for a red team run.
/// </summary>
public class RedTeamResponse
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? ApplicationScenario { get; set; }
    public int? TurnCount { get; set; }
    public bool? SimulationOnly { get; set; }
    public List<string?> AttackStrategies { get; set; } = new();
    public List<string?> RiskCategories { get; set; } = new();
}

/// <summary>
/// Request model for creating a red team run.
/// </summary>
public class CreateRedTeamRequest
{
    /// <summary>
    /// The Azure OpenAI model deployment to red-team
    /// (e.g. the name of your GPT-4o deployment).
    /// </summary>
    [Required]
    public required string ModelDeploymentName { get; set; }

    /// <summary>Human-readable display name for this red team run.</summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Description of the application scenario being tested
    /// (e.g. "Customer-facing support bot for a retail bank").
    /// </summary>
    public string? ApplicationScenario { get; set; }

    /// <summary>Number of turns per conversation. Defaults to the service default.</summary>
    public int? TurnCount { get; set; }

    /// <summary>
    /// When true, only simulated attacks are generated; no real model calls are made.
    /// Useful for reviewing attack prompts before a live run.
    /// </summary>
    public bool? SimulationOnly { get; set; }

    /// <summary>
    /// Attack strategies to apply (e.g. "Jailbreak", "PromptInjection", "DirectHarm").
    /// Defaults to a standard set when omitted.
    /// </summary>
    public List<string>? AttackStrategies { get; set; }

    /// <summary>
    /// Risk categories to probe (e.g. "Violence", "SexualContent", "HateSpeech").
    /// Defaults to all categories when omitted.
    /// </summary>
    public List<string>? RiskCategories { get; set; }
}
