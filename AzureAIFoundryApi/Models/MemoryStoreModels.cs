using System.ComponentModel.DataAnnotations;

namespace AzureAIFoundryApi.Models;

/// <summary>
/// Response model for a memory store.
/// </summary>
public class MemoryStoreResponse
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new memory store.
/// </summary>
public class CreateMemoryStoreRequest
{
    /// <summary>Unique name for the memory store.</summary>
    [Required]
    public required string Name { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Name of the chat model deployment used by this memory store
    /// (e.g. "gpt-4o").
    /// </summary>
    [Required]
    public required string ChatModelDeployment { get; set; }

    /// <summary>
    /// Name of the embedding model deployment used to index memories
    /// (e.g. "text-embedding-3-small").
    /// </summary>
    [Required]
    public required string EmbeddingModelDeployment { get; set; }
}

/// <summary>
/// Request model for updating a memory store's metadata.
/// </summary>
public class UpdateMemoryStoreRequest
{
    /// <summary>New description for the store.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request model for searching memories in a store.
/// </summary>
public class SearchMemoriesRequest
{
    /// <summary>
    /// The scope / query string to search with.
    /// This is typically a user ID, session ID, or natural-language topic.
    /// </summary>
    [Required]
    public required string Scope { get; set; }

    /// <summary>Maximum number of results to return (default: 5).</summary>
    public int MaxMemories { get; set; } = 5;
}

/// <summary>
/// A single search result from a memory store.
/// </summary>
public class MemorySearchResultResponse
{
    public string? Id { get; set; }
    public string? Content { get; set; }
}
