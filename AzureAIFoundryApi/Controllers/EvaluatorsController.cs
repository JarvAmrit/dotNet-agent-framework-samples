using System.ClientModel;
using Azure.AI.Projects;
using Azure.AI.Projects.Evaluation;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryApi.Controllers;

/// <summary>
/// Controller for browsing built-in and custom evaluator definitions in Azure AI Foundry.
/// Evaluators measure response quality dimensions such as coherence, relevance,
/// groundedness, and fluency.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EvaluatorsController : ControllerBase
{
    private readonly AIProjectClientFactory _clientFactory;
    private readonly ILogger<EvaluatorsController> _logger;

    public EvaluatorsController(AIProjectClientFactory clientFactory, ILogger<EvaluatorsController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Lists the latest version of every available evaluator in the project.
    /// Includes both built-in Azure AI evaluators and custom evaluators.
    /// </summary>
    /// <param name="type">Optional filter by evaluator type (e.g. "BuiltIn" or "Custom").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    public async Task<IActionResult> ListEvaluators(
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();
        var evaluatorsClient = client.Evaluators;

        EvaluatorType? evaluatorType = type is not null
            ? new EvaluatorType(type)
            : null;

        var evaluators = new List<EvaluatorVersionResponse>();
        await foreach (var evaluator in evaluatorsClient.GetLatestVersionsAsync(
            evaluatorType is null ? null : new ListVersionsRequestType(evaluatorType.Value.ToString()),
            limit: null,
            cancellationToken: cancellationToken))
        {
            evaluators.Add(MapToResponse(evaluator));
        }

        return Ok(evaluators);
    }

    /// <summary>
    /// Lists all versions of a specific evaluator.
    /// </summary>
    /// <param name="evaluatorName">The name of the evaluator.</param>
    /// <param name="type">Optional filter by evaluator type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{evaluatorName}/versions")]
    public async Task<IActionResult> ListEvaluatorVersions(
        string evaluatorName,
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();
        var evaluatorsClient = client.Evaluators;

        EvaluatorType? evaluatorType = type is not null ? new EvaluatorType(type) : null;

        var versions = new List<EvaluatorVersionResponse>();
        await foreach (var version in evaluatorsClient.GetVersionsAsync(
            evaluatorName,
            evaluatorType is null ? null : new ListVersionsRequestType(evaluatorType.Value.ToString()),
            limit: null,
            cancellationToken: cancellationToken))
        {
            versions.Add(MapToResponse(version));
        }

        return Ok(versions);
    }

    /// <summary>
    /// Gets a specific version of an evaluator.
    /// </summary>
    /// <param name="evaluatorName">The name of the evaluator.</param>
    /// <param name="version">The version to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{evaluatorName}/versions/{version}")]
    public async Task<IActionResult> GetEvaluatorVersion(
        string evaluatorName,
        string version,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.GetClient();
            var result = await client.Evaluators.GetVersionAsync(evaluatorName, version, cancellationToken: cancellationToken);
            return Ok(MapToResponse(result.Value));
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Evaluator version not found." });
        }
    }

    private static EvaluatorVersionResponse MapToResponse(EvaluatorVersion version) =>
        new()
        {
            Id = version.Id,
            Name = version.Name,
            DisplayName = version.DisplayName,
            Type = version.EvaluatorType.ToString(),
            CreatedAt = version.CreatedAt,
            ModifiedAt = version.ModifiedAt
        };
}
