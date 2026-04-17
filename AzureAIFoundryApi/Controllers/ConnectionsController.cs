using System.ClientModel;
using Azure.AI.Projects;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryApi.Controllers;

/// <summary>
/// Controller for listing and retrieving connections in Azure AI Foundry.
/// Connections link the Foundry project to Azure services such as Azure OpenAI,
/// Azure AI Search, Azure Blob Storage, and others.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConnectionsController : ControllerBase
{
    private readonly AIProjectClientFactory _clientFactory;
    private readonly ILogger<ConnectionsController> _logger;

    public ConnectionsController(AIProjectClientFactory clientFactory, ILogger<ConnectionsController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Lists all connections in the Azure AI Foundry project.
    /// </summary>
    /// <param name="type">
    /// Optional filter by connection type. Supported values: AzureOpenAI, AzureAISearch,
    /// AzureBlobStorage, AzureStorageAccount, CosmosDB, APIKey, ApplicationInsights, Custom, RemoteTool.
    /// </param>
    /// <param name="defaultOnly">
    /// When true, only default connections are returned. Defaults to false.
    /// </param>
    [HttpGet]
    public async Task<IActionResult> ListConnections(
        [FromQuery] string? type = null,
        [FromQuery] bool? defaultOnly = null)
    {
        var client = _clientFactory.GetClient();
        var connectionsClient = client.Connections;

        ConnectionType? connectionType = type is not null ? new ConnectionType(type) : null;

        var connections = new List<ConnectionResponse>();
        await foreach (var connection in connectionsClient.GetConnectionsAsync(connectionType, defaultOnly))
        {
            connections.Add(MapToResponse(connection));
        }

        return Ok(connections);
    }

    /// <summary>
    /// Gets the default connection of a given type in the Azure AI Foundry project.
    /// </summary>
    /// <param name="type">
    /// Optional connection type filter. Supported values: AzureOpenAI, AzureAISearch,
    /// AzureBlobStorage, AzureStorageAccount, CosmosDB, APIKey, ApplicationInsights, Custom, RemoteTool.
    /// </param>
    /// <param name="includeCredentials">
    /// When true, credentials are included in the response. Defaults to false.
    /// </param>
    [HttpGet("default")]
    public async Task<IActionResult> GetDefaultConnection(
        [FromQuery] string? type = null,
        [FromQuery] bool includeCredentials = false)
    {
        ConnectionType? connectionType = type is not null ? new ConnectionType(type) : null;

        var client = _clientFactory.GetClient();
        var connectionsClient = client.Connections;

        var connection = await connectionsClient.GetDefaultConnectionAsync(connectionType, includeCredentials);

        if (includeCredentials)
            return Ok(MapToResponseWithCredentials(connection));

        return Ok(MapToResponse(connection));
    }

    /// <summary>
    /// Gets a specific connection by name.
    /// </summary>
    /// <param name="connectionName">The name of the connection.</param>
    /// <param name="includeCredentials">
    /// When true, credentials are included in the response. Defaults to false.
    /// </param>
    [HttpGet("{connectionName}")]
    public async Task<IActionResult> GetConnection(
        string connectionName,
        [FromQuery] bool includeCredentials = false)
    {
        try
        {
            var client = _clientFactory.GetClient();
            var connectionsClient = client.Connections;

            var connection = await connectionsClient.GetConnectionAsync(connectionName, includeCredentials);

            if (includeCredentials)
                return Ok(MapToResponseWithCredentials(connection));

            return Ok(MapToResponse(connection));
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Connection not found." });
        }
    }

    private static string GetCredentialType(AIProjectConnectionBaseCredential? credentials) =>
        credentials switch
        {
            AIProjectConnectionApiKeyCredential => "ApiKey",
            AIProjectConnectionEntraIdCredential => "EntraId",
            AIProjectConnectionSasCredential => "SAS",
            AIProjectConnectionCustomCredential => "Custom",
            NoAuthenticationCredentials => "None",
            AgenticIdentityPreviewCredentials => "AgenticIdentityPreview",
            null => "None",
            _ => credentials.GetType().Name
        };

    private static ConnectionResponse MapToResponse(AIProjectConnection connection) =>
        new()
        {
            Name = connection.Name,
            Type = connection.Type.ToString(),
            Target = connection.Target,
            IsDefault = connection.IsDefault,
            CredentialType = GetCredentialType(connection.Credentials)
        };

    private static ConnectionWithCredentialsResponse MapToResponseWithCredentials(AIProjectConnection connection)
    {
        var response = new ConnectionWithCredentialsResponse
        {
            Name = connection.Name,
            Type = connection.Type.ToString(),
            Target = connection.Target,
            IsDefault = connection.IsDefault,
            CredentialType = GetCredentialType(connection.Credentials)
        };

        if (connection.Credentials is AIProjectConnectionApiKeyCredential apiKeyCredential)
            response.ApiKey = apiKeyCredential.ApiKey;
        else if (connection.Credentials is AIProjectConnectionSasCredential sasCredential)
            response.SasToken = sasCredential.SasToken;

        return response;
    }
}
