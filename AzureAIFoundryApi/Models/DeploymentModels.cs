namespace AzureAIFoundryApi.Models;

/// <summary>
/// Response model representing a model deployment in Azure AI Foundry.
/// </summary>
public class DeploymentResponse
{
    public string? Name { get; set; }
    public string? ModelId { get; set; }
    public string? ModelPublisher { get; set; }
    public string? Type { get; set; }
}
