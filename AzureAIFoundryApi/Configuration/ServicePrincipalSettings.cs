namespace AzureAIFoundryApi.Configuration;

/// <summary>
/// Configuration settings for authenticating via a Service Principal (Entra ID App Registration).
/// When these values are provided, the API will use ClientSecretCredential instead of DefaultAzureCredential.
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
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Returns true if all required fields are populated for Service Principal authentication.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TenantId) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}
