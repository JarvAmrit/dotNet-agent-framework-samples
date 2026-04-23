using System.ClientModel;
using Azure.AI.Projects.Memory;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryApi.Controllers;

/// <summary>
/// Controller for managing memory stores in Azure AI Foundry.
/// Memory stores provide persistent, searchable storage for agent conversation context,
/// user profiles, and other domain knowledge used across sessions.
/// </summary>
[ApiController]
[Route("api/memory-stores")]
public class MemoryStoresController : ControllerBase
{
    private readonly AIProjectClientFactory _clientFactory;
    private readonly ILogger<MemoryStoresController> _logger;

    public MemoryStoresController(AIProjectClientFactory clientFactory, ILogger<MemoryStoresController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Lists all memory stores in the project.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListMemoryStores(CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();
        var stores = new List<MemoryStoreResponse>();

        await foreach (var store in client.MemoryStores.GetMemoryStoresAsync(
            limit: null, order: null, after: null, before: null, cancellationToken: cancellationToken))
        {
            stores.Add(MapToResponse(store));
        }

        return Ok(stores);
    }

    /// <summary>
    /// Gets a specific memory store by name.
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> GetMemoryStore(string name, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.GetClient();
            var result = await client.MemoryStores.GetMemoryStoreAsync(name, cancellationToken: cancellationToken);
            return Ok(MapToResponse(result.Value));
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Memory store not found." });
        }
    }

    /// <summary>
    /// Creates a new memory store.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateMemoryStore(
        [FromBody] CreateMemoryStoreRequest request,
        CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();

        var definition = new MemoryStoreDefaultDefinition(
            request.ChatModelDeployment,
            request.EmbeddingModelDeployment);

        var result = await client.MemoryStores.CreateMemoryStoreAsync(
            request.Name,
            definition,
            request.Description,
            metadata: null,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Created memory store '{Name}'", Sanitize(request.Name));
        return CreatedAtAction(nameof(GetMemoryStore), new { name = result.Value.Name }, MapToResponse(result.Value));
    }

    /// <summary>
    /// Updates the description or metadata of a memory store.
    /// </summary>
    [HttpPatch("{name}")]
    public async Task<IActionResult> UpdateMemoryStore(
        string name,
        [FromBody] UpdateMemoryStoreRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.GetClient();
            var result = await client.MemoryStores.UpdateMemoryStoreAsync(
                name, request.Description, metadata: null, cancellationToken: cancellationToken);

            _logger.LogInformation("Updated memory store '{Name}'", Sanitize(name));
            return Ok(MapToResponse(result.Value));
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Memory store not found." });
        }
    }

    /// <summary>
    /// Deletes a memory store and all its stored memories.
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> DeleteMemoryStore(string name, CancellationToken cancellationToken)
    {
        var client = _clientFactory.GetClient();
        await client.MemoryStores.DeleteMemoryStoreAsync(name, cancellationToken: cancellationToken);
        _logger.LogInformation("Deleted memory store '{Name}'", Sanitize(name));
        return NoContent();
    }

    /// <summary>
    /// Searches a memory store for items relevant to the provided scope/query.
    /// </summary>
    [HttpPost("{name}/search")]
    public async Task<IActionResult> SearchMemories(
        string name,
        [FromBody] SearchMemoriesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.GetClient();

            var options = new MemorySearchOptions(request.Scope)
            {
                ResultOptions = new MemorySearchResultOptions { MaxMemories = request.MaxMemories }
            };

            var result = await client.MemoryStores.SearchMemoriesAsync(name, options, cancellationToken: cancellationToken);

            var items = result.Value.Memories
                .Select(item => new MemorySearchResultResponse
                {
                    Id = item.MemoryItem?.MemoryId,
                    Content = item.MemoryItem?.Content
                })
                .ToList();

            return Ok(new { items });
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Memory store not found." });
        }
    }

    private static MemoryStoreResponse MapToResponse(MemoryStore store) =>
        new()
        {
            Id = store.Id,
            Name = store.Name,
            Description = store.Description,
            CreatedAt = store.CreatedAt,
            UpdatedAt = store.UpdatedAt
        };

    private static string Sanitize(string? value) =>
        (value ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal)
                               .Replace("\n", string.Empty, StringComparison.Ordinal);
}
