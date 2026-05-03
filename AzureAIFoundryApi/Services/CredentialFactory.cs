using Azure.Core;
using Azure.Identity;

namespace AzureAIFoundryApi.Services;

/// <summary>
/// Factory for creating Azure TokenCredential instances.
/// Uses DefaultAzureCredential, which supports managed identity when running in Azure
/// and the developer's local Azure credentials during local development.
/// </summary>
public class CredentialFactory
{
    private readonly ILogger<CredentialFactory> _logger;

    public CredentialFactory(ILogger<CredentialFactory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a DefaultAzureCredential that works with managed identity in Azure
    /// and local developer credentials for local development.
    /// </summary>
    public TokenCredential CreateCredential()
    {
        _logger.LogInformation("Using DefaultAzureCredential for authentication");
        return new DefaultAzureCredential();
    }
}
