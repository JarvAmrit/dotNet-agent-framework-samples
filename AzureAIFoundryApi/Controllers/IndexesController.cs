using System.ClientModel;
using Azure.AI.Projects;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryApi.Controllers;

/// <summary>
/// Controller for managing vector search indexes in Azure AI Foundry.
/// Indexes can be backed by Azure AI Search, Managed Azure AI Search, or Azure Cosmos DB.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IndexesController : ControllerBase
{
    private readonly AIProjectClientFactory _clientFactory;
    private readonly ILogger<IndexesController> _logger;

    public IndexesController(AIProjectClientFactory clientFactory, ILogger<IndexesController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Lists all indexes registered in the Azure AI Foundry project.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListIndexes()
    {
        var client = _clientFactory.GetClient();
        var indexesClient = client.Indexes;

        var indexes = new List<IndexResponse>();
        await foreach (var index in indexesClient.GetIndexesAsync())
        {
            indexes.Add(MapToResponse(index));
        }

        return Ok(indexes);
    }

    /// <summary>
    /// Lists all versions of a specific index.
    /// </summary>
    [HttpGet("{indexName}/versions")]
    public async Task<IActionResult> ListIndexVersions(string indexName)
    {
        var indexes = new List<IndexResponse>();
        var client = _clientFactory.GetClient();
        var indexesClient = client.Indexes;

        await foreach (var index in indexesClient.GetIndexVersionsAsync(indexName))
        {
            indexes.Add(MapToResponse(index));
        }

        return Ok(indexes);
    }

    /// <summary>
    /// Gets a specific version of an index.
    /// </summary>
    [HttpGet("{indexName}/versions/{indexVersion}")]
    public async Task<IActionResult> GetIndexVersion(string indexName, string indexVersion)
    {
        try
        {
            var client = _clientFactory.GetClient();
            var indexesClient = client.Indexes;

            var result = await indexesClient.GetIndexAsync(indexName, indexVersion);
            return Ok(MapToResponse(result.Value));
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Index version not found." });
        }
    }

    /// <summary>
    /// Creates or updates an Azure AI Search index version in the Foundry project.
    /// </summary>
    [HttpPut("{indexName}/versions/{indexVersion}/azure-search")]
    public async Task<IActionResult> CreateOrUpdateAzureSearchIndex(
        string indexName,
        string indexVersion,
        [FromBody] CreateAzureAISearchIndexRequest request)
    {
        var client = _clientFactory.GetClient();
        var indexesClient = client.Indexes;

        var index = new AzureAISearchIndex(request.ConnectionName, request.IndexName)
        {
            Description = request.Description
        };

        var result = await indexesClient.CreateOrUpdateAsync(indexName, indexVersion, index);

        _logger.LogInformation("Created/updated Azure AI Search index version successfully");
        return Ok(MapToResponse(result.Value));
    }

    /// <summary>
    /// Creates or updates a Managed Azure AI Search index version in the Foundry project.
    /// </summary>
    [HttpPut("{indexName}/versions/{indexVersion}/managed")]
    public async Task<IActionResult> CreateOrUpdateManagedIndex(
        string indexName,
        string indexVersion,
        [FromBody] CreateManagedIndexRequest request)
    {
        var client = _clientFactory.GetClient();
        var indexesClient = client.Indexes;

        var index = new ManagedAzureAISearchIndex(request.VectorStoreId)
        {
            Description = request.Description
        };

        var result = await indexesClient.CreateOrUpdateAsync(indexName, indexVersion, index);

        _logger.LogInformation("Created/updated managed index version successfully");
        return Ok(MapToResponse(result.Value));
    }

    /// <summary>
    /// Deletes a specific version of an index.
    /// </summary>
    [HttpDelete("{indexName}/versions/{indexVersion}")]
    public async Task<IActionResult> DeleteIndexVersion(string indexName, string indexVersion)
    {
        var client = _clientFactory.GetClient();
        var indexesClient = client.Indexes;

        await indexesClient.DeleteAsync(indexName, indexVersion);

        _logger.LogInformation("Deleted index version successfully");
        return NoContent();
    }

    private static IndexResponse MapToResponse(AIProjectIndex index)
    {
        var response = new IndexResponse
        {
            Id = index.Id,
            Name = index.Name,
            Version = index.Version,
            Description = index.Description
        };

        if (index is AzureAISearchIndex azureSearch)
        {
            response.Type = "AzureAISearch";
            response.ConnectionName = azureSearch.ConnectionName;
            response.IndexName = azureSearch.IndexName;
        }
        else if (index is ManagedAzureAISearchIndex managed)
        {
            response.Type = "ManagedAzureAISearch";
            response.VectorStoreId = managed.VectorStoreId;
        }
        else if (index is AIProjectCosmosDBIndex cosmosDb)
        {
            response.Type = "CosmosDB";
            response.ConnectionName = cosmosDb.ConnectionName;
            response.DatabaseName = cosmosDb.DatabaseName;
            response.ContainerName = cosmosDb.ContainerName;
        }

        return response;
    }
}
