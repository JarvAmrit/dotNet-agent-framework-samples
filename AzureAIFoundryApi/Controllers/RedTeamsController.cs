using System.ClientModel;
using Azure.AI.Projects.Evaluation;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryApi.Controllers;

/// <summary>
/// Controller for managing red team operations in Azure AI Foundry.
/// Red teaming simulates adversarial attacks against an agent to identify safety risks,
/// potential harms, and prompt injection vulnerabilities before deployment.
/// </summary>
[ApiController]
[Route("api/red-teams")]
public class RedTeamsController : ControllerBase
{
    private readonly AIProjectClientFactory _clientFactory;
    private readonly ILogger<RedTeamsController> _logger;

    public RedTeamsController(AIProjectClientFactory clientFactory, ILogger<RedTeamsController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Lists all red team runs in the project.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListRedTeams(CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();
        var redTeams = new List<RedTeamResponse>();

        await foreach (var redTeam in client.RedTeams.GetAllAsync(cancellationToken: cancellationToken))
        {
            redTeams.Add(MapToResponse(redTeam));
        }

        return Ok(redTeams);
    }

    /// <summary>
    /// Gets a specific red team run by name.
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> GetRedTeam(string name, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.GetClient();
            var result = await client.RedTeams.GetAsync(name, cancellationToken: cancellationToken);
            return Ok(MapToResponse(result.Value));
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Red team run not found." });
        }
    }

    /// <summary>
    /// Creates and starts a new red team run against a target model deployment.
    /// The run simulates adversarial prompts across the specified risk categories
    /// and attack strategies, then reports on vulnerabilities found.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRedTeam(
        [FromBody] CreateRedTeamRequest request,
        CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();

        var target = new AzureOpenAIModelConfiguration(request.ModelDeploymentName);
        var redTeam = new RedTeam(target)
        {
            DisplayName = request.DisplayName,
            ApplicationScenario = request.ApplicationScenario,
            TurnCount = request.TurnCount,
            IsSimulationOnly = request.SimulationOnly
        };

        foreach (var strategy in request.AttackStrategies ?? [])
            redTeam.AttackStrategies.Add(new AttackStrategy(strategy));

        foreach (var category in request.RiskCategories ?? [])
            redTeam.RiskCategories.Add(new RiskCategory(category));

        var result = await client.RedTeams.CreateAsync(redTeam, cancellationToken: cancellationToken);

        _logger.LogInformation("Created red team run '{Name}'", result.Value.Name);
        return CreatedAtAction(nameof(GetRedTeam), new { name = result.Value.Name }, MapToResponse(result.Value));
    }

    private static RedTeamResponse MapToResponse(RedTeam redTeam) =>
        new()
        {
            Name = redTeam.Name,
            DisplayName = redTeam.DisplayName,
            ApplicationScenario = redTeam.ApplicationScenario,
            TurnCount = redTeam.TurnCount,
            SimulationOnly = redTeam.IsSimulationOnly,
            AttackStrategies = redTeam.AttackStrategies.Select(a => (string?)a.ToString()).ToList(),
            RiskCategories = redTeam.RiskCategories.Select(r => (string?)r.ToString()).ToList()
        };
}
