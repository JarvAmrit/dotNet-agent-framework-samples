using Azure.AI.Projects;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryApi.Controllers;

/// <summary>
/// Controller for managing model deployments in Azure AI Foundry.
/// Lists and retrieves model deployments available in the Foundry project.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DeploymentsController : ControllerBase
{
    private readonly AIProjectClientFactory _clientFactory;
    private readonly ILogger<DeploymentsController> _logger;

    public DeploymentsController(AIProjectClientFactory clientFactory, ILogger<DeploymentsController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Lists all model deployments in the Azure AI Foundry project.
    /// </summary>
    /// <param name="modelPublisher">Optional filter by model publisher (e.g., "Microsoft", "OpenAI").</param>
    /// <param name="modelName">Optional filter by model name.</param>
    /// <param name="deploymentType">Optional filter by deployment type (e.g., "ModelDeployment").</param>
    [HttpGet]
    public IActionResult ListDeployments(
        [FromQuery] string? modelPublisher = null,
        [FromQuery] string? modelName = null,
        [FromQuery] string? deploymentType = null)
    {
        var client = _clientFactory.GetClient();
        var deploymentsClient = client.Deployments;

        AIProjectDeploymentType? typeFilter = null;
        if (!string.IsNullOrWhiteSpace(deploymentType))
        {
            typeFilter = new AIProjectDeploymentType(deploymentType);
        }

        var deployments = new List<DeploymentResponse>();
        foreach (var deployment in deploymentsClient.GetDeployments(
            modelPublisher: modelPublisher,
            modelName: modelName,
            deploymentType: typeFilter))
        {
            deployments.Add(MapToResponse(deployment));
        }

        return Ok(deployments);
    }

    /// <summary>
    /// Gets details of a specific model deployment by name.
    /// </summary>
    [HttpGet("{deploymentName}")]
    public IActionResult GetDeployment(string deploymentName)
    {
        var client = _clientFactory.GetClient();
        var deploymentsClient = client.Deployments;

        var result = deploymentsClient.GetDeployment(deploymentName);
        return Ok(MapToResponse(result.Value));
    }

    private static DeploymentResponse MapToResponse(AIProjectDeployment deployment)
    {
        var response = new DeploymentResponse
        {
            Name = deployment.Name
        };

        if (deployment is ModelDeployment modelDeployment)
        {
            response.Type = "ModelDeployment";
            response.ModelId = modelDeployment.ModelName;
            response.ModelPublisher = modelDeployment.ModelPublisher;
        }

        return response;
    }
}
