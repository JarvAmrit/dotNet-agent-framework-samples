namespace AzureAIFoundryApi.Configuration;

/// <summary>
/// Configuration settings for connecting to Azure AI Foundry.
/// </summary>
public class AzureFoundrySettings
{
    public const string SectionName = "AzureFoundry";

    /// <summary>
    /// The Azure AI Foundry project endpoint URL.
    /// Example: https://your-project.services.ai.azure.com
    /// </summary>
    public required string ProjectEndpoint { get; set; }

    /// <summary>
    /// Optional: The default model deployment name to use when creating agents.
    /// </summary>
    public string? DefaultModelDeploymentName { get; set; }
}
