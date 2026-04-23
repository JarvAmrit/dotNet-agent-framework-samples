using System.ClientModel;
using Azure.AI.Projects;
using Azure.AI.Projects.Evaluation;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryApi.Controllers;

/// <summary>
/// Controller for managing continuous evaluation rules in Azure AI Foundry.
/// Evaluation rules define when and how agent responses should be automatically evaluated
/// (e.g., on every message, on a schedule, or when specific conditions are met).
/// </summary>
[ApiController]
[Route("api/evaluation-rules")]
public class EvaluationRulesController : ControllerBase
{
    private readonly AIProjectClientFactory _clientFactory;
    private readonly ILogger<EvaluationRulesController> _logger;

    public EvaluationRulesController(AIProjectClientFactory clientFactory, ILogger<EvaluationRulesController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Lists all evaluation rules in the project.
    /// </summary>
    /// <param name="actionType">Optional filter by action type (e.g. "ContinuousEvaluation").</param>
    /// <param name="agentName">Optional filter by the agent the rule applies to.</param>
    /// <param name="enabledOnly">When true, only enabled rules are returned.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    public async Task<IActionResult> ListEvaluationRules(
        [FromQuery] string? actionType,
        [FromQuery] string? agentName,
        [FromQuery] bool? enabledOnly,
        CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();
        var rulesClient = client.EvaluationRules;

        EvaluationRuleActionType? actionTypeFilter = actionType is not null
            ? new EvaluationRuleActionType(actionType)
            : null;

        var rules = new List<EvaluationRuleResponse>();
        await foreach (var rule in rulesClient.GetAllAsync(actionTypeFilter, agentName, enabledOnly, cancellationToken: cancellationToken))
        {
            rules.Add(MapToResponse(rule));
        }

        return Ok(rules);
    }

    /// <summary>
    /// Gets a specific evaluation rule by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvaluationRule(string id, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.GetClient();
            var result = await client.EvaluationRules.GetAsync(id, cancellationToken: cancellationToken);
            return Ok(MapToResponse(result.Value));
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Evaluation rule not found." });
        }
    }

    /// <summary>
    /// Creates or updates an evaluation rule.
    /// </summary>
    /// <param name="id">The ID to assign to the rule (used as idempotency key).</param>
    /// <param name="request">The evaluation rule configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{id}")]
    public async Task<IActionResult> CreateOrUpdateEvaluationRule(
        string id,
        [FromBody] CreateEvaluationRuleRequest request,
        CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();

        var action = new ContinuousEvaluationRuleAction(request.EvaluatorName);

        var eventType = new EvaluationRuleEventType(request.EventType);

        var rule = new EvaluationRule(action, eventType, request.Enabled)
        {
            DisplayName = request.DisplayName,
            Description = request.Description
        };

        var result = await client.EvaluationRules.CreateOrUpdateAsync(
            id, rule, foundryFeatures: null, cancellationToken: cancellationToken);

        _logger.LogInformation("Created/updated evaluation rule '{Id}'", Sanitize(id));
        return Ok(MapToResponse(result.Value));
    }

    /// <summary>
    /// Deletes an evaluation rule by ID.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvaluationRule(string id, CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();
        await client.EvaluationRules.DeleteAsync(id, cancellationToken: cancellationToken);
        _logger.LogInformation("Deleted evaluation rule '{Id}'", Sanitize(id));
        return NoContent();
    }

    private static EvaluationRuleResponse MapToResponse(EvaluationRule rule) =>
        new()
        {
            Id = rule.Id,
            DisplayName = rule.DisplayName,
            Description = rule.Description,
            EventType = rule.EventType.ToString(),
            Enabled = rule.Enabled
        };

    private static string Sanitize(string? value) =>
        (value ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal)
                               .Replace("\n", string.Empty, StringComparison.Ordinal);
}
