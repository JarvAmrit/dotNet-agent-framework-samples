using System.ComponentModel.DataAnnotations;

namespace AzureAIFoundryApi.Models;

/// <summary>
/// Response model representing a connection in Azure AI Foundry.
/// </summary>
public class ConnectionResponse
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Target { get; set; }
    public bool IsDefault { get; set; }
    public string? CredentialType { get; set; }
}

/// <summary>
/// Response model representing a connection with its credentials.
/// </summary>
public class ConnectionWithCredentialsResponse : ConnectionResponse
{
    /// <summary>
    /// The API key, if the connection uses ApiKey authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// The SAS token, if the connection uses SAS authentication.
    /// </summary>
    public string? SasToken { get; set; }
}
