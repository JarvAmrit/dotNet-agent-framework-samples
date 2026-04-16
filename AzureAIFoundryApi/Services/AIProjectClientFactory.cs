using Azure.AI.Projects;
using Azure.Identity;
using AzureAIFoundryApi.Configuration;
using Microsoft.Extensions.Options;

namespace AzureAIFoundryApi.Services;

/// <summary>
/// Factory for creating and configuring the AIProjectClient from the Azure.AI.Projects SDK.
/// </summary>
public class AIProjectClientFactory
{
    private readonly AzureFoundrySettings _foundrySettings;
    private readonly CredentialFactory _credentialFactory;
    private readonly ILogger<AIProjectClientFactory> _logger;
    private AIProjectClient? _cachedClient;
    private readonly object _lock = new();

    public AIProjectClientFactory(
        IOptions<AzureFoundrySettings> foundrySettings,
        CredentialFactory credentialFactory,
        ILogger<AIProjectClientFactory> logger)
    {
        _foundrySettings = foundrySettings.Value;
        _credentialFactory = credentialFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a singleton AIProjectClient instance.
    /// </summary>
    public AIProjectClient GetClient()
    {
        if (_cachedClient is not null)
            return _cachedClient;

        lock (_lock)
        {
            if (_cachedClient is not null)
                return _cachedClient;

            var credential = _credentialFactory.CreateCredential();
            var endpoint = new Uri(_foundrySettings.ProjectEndpoint);

            _logger.LogInformation("Creating AIProjectClient for endpoint: {Endpoint}", endpoint);

            _cachedClient = new AIProjectClient(endpoint, credential);
            return _cachedClient;
        }
    }
}
