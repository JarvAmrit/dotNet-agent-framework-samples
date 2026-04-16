namespace AzureAIFoundryApi.Configuration;

/// <summary>
/// Configuration settings for authenticating via a Service Principal (Entra ID App Registration).
/// Supports both client secret and certificate-based authentication.
/// When certificate fields are provided, ClientCertificateCredential is used.
/// When only client secret fields are provided, ClientSecretCredential is used.
/// Otherwise, DefaultAzureCredential is used for local development and managed identity scenarios.
/// </summary>
public class ServicePrincipalSettings
{
    public const string SectionName = "ServicePrincipal";

    /// <summary>
    /// The Azure AD / Entra ID Tenant ID.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// The Application (Client) ID of the registered app in Entra ID.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// The Client Secret for the registered app.
    /// Used when authenticating with a client secret instead of a certificate.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// The thumbprint of the certificate to use for authentication.
    /// The certificate will be looked up in the current user's personal certificate store (CurrentUser\My),
    /// then the local machine store (LocalMachine\My).
    /// Either CertificateThumbprint or CertificateFilePath must be set to enable certificate authentication.
    /// </summary>
    public string? CertificateThumbprint { get; set; }

    /// <summary>
    /// The file path to a PFX certificate file for authentication.
    /// Takes precedence over CertificateThumbprint if both are provided.
    /// Either CertificateFilePath or CertificateThumbprint must be set to enable certificate authentication.
    /// </summary>
    public string? CertificateFilePath { get; set; }

    /// <summary>
    /// Optional password for the PFX certificate file specified in CertificateFilePath.
    /// </summary>
    public string? CertificatePassword { get; set; }

    /// <summary>
    /// Returns true if all required fields are populated for client secret authentication.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TenantId) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);

    /// <summary>
    /// Returns true if all required fields are populated for certificate-based authentication.
    /// </summary>
    public bool IsCertificateConfigured =>
        !string.IsNullOrWhiteSpace(TenantId) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        (!string.IsNullOrWhiteSpace(CertificateThumbprint) || !string.IsNullOrWhiteSpace(CertificateFilePath));
}
