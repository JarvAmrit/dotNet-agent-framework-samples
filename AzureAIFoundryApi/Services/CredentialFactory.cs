using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using Azure.Identity;
using AzureAIFoundryApi.Configuration;
using Microsoft.Extensions.Options;

namespace AzureAIFoundryApi.Services;

/// <summary>
/// Factory for creating Azure TokenCredential instances.
/// Credential selection priority:
/// 1. ClientCertificateCredential – when TenantId, ClientId, and a certificate (thumbprint or file path) are configured.
/// 2. ClientSecretCredential – when TenantId, ClientId, and ClientSecret are configured.
/// 3. DefaultAzureCredential – fallback for local development and managed identity scenarios.
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
        if (_spSettings.IsCertificateConfigured)
        {
            _logger.LogInformation("Using Service Principal certificate authentication");
            var certificate = LoadCertificate();
            return new ClientCertificateCredential(
                _spSettings.TenantId,
                _spSettings.ClientId,
                certificate);
        }

        if (_spSettings.IsConfigured)
        {
            _logger.LogInformation("Using Service Principal client secret authentication");
            return new ClientSecretCredential(
                _spSettings.TenantId,
                _spSettings.ClientId,
                _spSettings.ClientSecret);
        }

        _logger.LogInformation("Using DefaultAzureCredential for authentication");
        return new DefaultAzureCredential();
    }

    /// <summary>
    /// Loads an X509Certificate2 from a PFX file (if CertificateFilePath is set)
    /// or from the system certificate store by thumbprint (if CertificateThumbprint is set).
    /// </summary>
    private X509Certificate2 LoadCertificate()
    {
        if (!string.IsNullOrWhiteSpace(_spSettings.CertificateFilePath))
        {
            _logger.LogInformation("Loading certificate from file: {CertificateFilePath}", _spSettings.CertificateFilePath);
            return X509CertificateLoader.LoadPkcs12FromFile(
                _spSettings.CertificateFilePath,
                _spSettings.CertificatePassword);
        }

        var thumbprint = _spSettings.CertificateThumbprint!.Trim();
        _logger.LogInformation("Loading certificate by thumbprint from certificate store");

        // Search CurrentUser\My first, then LocalMachine\My
        var storeLocations = new[] { StoreLocation.CurrentUser, StoreLocation.LocalMachine };
        foreach (var storeLocation in storeLocations)
        {
            using var store = new X509Store(StoreName.My, storeLocation);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            var matches = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: true);
            if (matches.Count > 0)
            {
                if (matches.Count > 1)
                {
                    _logger.LogWarning("Multiple certificates ({Count}) found with thumbprint '{Thumbprint}' in {StoreLocation}\\My store; using the first match",
                        matches.Count, thumbprint, storeLocation);
                }
                else
                {
                    _logger.LogInformation("Certificate found in {StoreLocation}\\My store", storeLocation);
                }
                return matches[0];
            }
        }

        throw new InvalidOperationException(
            $"Certificate with thumbprint '{thumbprint}' was not found in CurrentUser\\My or LocalMachine\\My certificate stores.");
    }
}
