using Azure.Core;
using Azure.Identity;
using AzureAIFoundryApi.Configuration;
using Microsoft.Extensions.Options;

namespace AzureAIFoundryApi.Services;

/// <summary>
/// Factory for creating Azure TokenCredential instances.
/// Uses Service Principal (ClientSecretCredential) when configured,
/// otherwise falls back to DefaultAzureCredential for local development and managed identity scenarios.
/// </summary>
public class CredentialFactory
{
    private readonly ServicePrincipalSettings _spSettings;
    private readonly ILogger<CredentialFactory> _logger;

    public CredentialFactory(IOptions<ServicePrincipalSettings> spSettings, ILogger<CredentialFactory> logger)
    {
        _spSettings = spSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates the appropriate TokenCredential based on configuration.
    /// </summary>
    public TokenCredential CreateCredential()
    {
        if (_spSettings.IsConfigured)
        {
            _logger.LogInformation(
                "Using Service Principal authentication (TenantId: {TenantId}, ClientId: {ClientId})",
                _spSettings.TenantId,
                _spSettings.ClientId);

            return new ClientSecretCredential(
                _spSettings.TenantId,
                _spSettings.ClientId,
                _spSettings.ClientSecret);
        }

        _logger.LogInformation("Using DefaultAzureCredential for authentication");
        return new DefaultAzureCredential();
    }
}
