using System.Collections.Concurrent;
using Azure.AI.Projects;
using Azure.Identity;
using AzureAIFoundryApi.Services;

namespace AzureAIFoundryApi.Services;

/// <summary>
/// Factory for creating and caching AIProjectClient instances keyed by project endpoint.
/// Each caller supplies the project endpoint URL at call time, allowing the API to serve
/// multiple Azure AI Foundry projects without restarting.
/// Authentication uses DefaultAzureCredential, which resolves to managed identity when
/// running in Azure and to the developer's local Azure credentials during local development.
/// </summary>
public class AIProjectClientFactory
{
    private readonly CredentialFactory _credentialFactory;
    private readonly ILogger<AIProjectClientFactory> _logger;
    private readonly ConcurrentDictionary<string, AIProjectClient> _clientCache = new(StringComparer.OrdinalIgnoreCase);

    public AIProjectClientFactory(
        CredentialFactory credentialFactory,
        ILogger<AIProjectClientFactory> logger)
    {
        _credentialFactory = credentialFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates an AIProjectClient for the specified project endpoint.
    /// Clients are cached per endpoint so that repeated calls with the same endpoint
    /// reuse the same instance.
    /// </summary>
    /// <param name="projectEndpoint">
    /// The Azure AI Foundry project endpoint URL.
    /// Example: https://your-project.services.ai.azure.com
    /// </param>
    public AIProjectClient GetClient(string projectEndpoint)
    {
        if (string.IsNullOrWhiteSpace(projectEndpoint))
            throw new ArgumentException(
                "A valid project endpoint must be provided (e.g., https://your-project.services.ai.azure.com).",
                nameof(projectEndpoint));

        Uri endpoint;
        try
        {
            endpoint = new Uri(projectEndpoint);
        }
        catch (UriFormatException ex)
        {
            throw new ArgumentException(
                "The project endpoint must be a valid absolute URL (e.g., https://your-project.services.ai.azure.com).",
                nameof(projectEndpoint), ex);
        }

        return _clientCache.GetOrAdd(projectEndpoint, _ =>
        {
            var credential = _credentialFactory.CreateCredential();
            _logger.LogInformation("Creating AIProjectClient for endpoint: {Endpoint}", SanitizeForLog(endpoint.AbsoluteUri));
            return new AIProjectClient(endpoint, credential);
        });
    }

    /// <summary>
    /// Removes newline characters from a value before it is written to a log entry
    /// to prevent log-forging attacks.
    /// </summary>
    private static string SanitizeForLog(string value) =>
        value.Replace("\r", string.Empty, StringComparison.Ordinal)
             .Replace("\n", string.Empty, StringComparison.Ordinal);
}
