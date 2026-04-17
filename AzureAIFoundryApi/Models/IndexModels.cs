using System.ComponentModel.DataAnnotations;

namespace AzureAIFoundryApi.Models;

/// <summary>
/// Response model representing an index in Azure AI Foundry.
/// </summary>
public class IndexResponse
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }

    /// <summary>
    /// For AzureAISearch indexes: the connection name.
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// For AzureAISearch indexes: the underlying search index name.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// For ManagedAzureAISearch indexes: the vector store ID.
    /// </summary>
    public string? VectorStoreId { get; set; }

    /// <summary>
    /// For CosmosDB indexes: the database name.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// For CosmosDB indexes: the container name.
    /// </summary>
    public string? ContainerName { get; set; }
}

/// <summary>
/// Request model for creating or updating an AzureAISearch index version.
/// </summary>
public class CreateAzureAISearchIndexRequest
{
    /// <summary>
    /// The name of the Azure AI Foundry connection to the Azure AI Search service.
    /// </summary>
    [Required]
    public required string ConnectionName { get; set; }

    /// <summary>
    /// The name of the underlying Azure AI Search index.
    /// </summary>
    [Required]
    public required string IndexName { get; set; }

    /// <summary>
    /// Optional description for the index.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request model for creating or updating a Managed Azure AI Search index version.
/// </summary>
public class CreateManagedIndexRequest
{
    /// <summary>
    /// The vector store ID for the managed Azure AI Search index.
    /// </summary>
    [Required]
    public required string VectorStoreId { get; set; }

    /// <summary>
    /// Optional description for the index.
    /// </summary>
    public string? Description { get; set; }
}
