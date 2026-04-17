using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.Projects;
using AzureAIFoundryApi.Models;
using AzureAIFoundryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureAIFoundryApi.Controllers;

/// <summary>
/// Controller for managing datasets in Azure AI Foundry.
/// Datasets represent versioned references to data stored in Azure Storage,
/// used for training, evaluation, and fine-tuning workflows.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DatasetsController : ControllerBase
{
    private readonly AIProjectClientFactory _clientFactory;
    private readonly ILogger<DatasetsController> _logger;

    public DatasetsController(AIProjectClientFactory clientFactory, ILogger<DatasetsController> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Lists all datasets registered in the Azure AI Foundry project.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListDatasets()
    {
        var client = _clientFactory.GetClient();
        var datasetsClient = client.Datasets;

        var datasets = new List<DatasetResponse>();
        await foreach (var dataset in datasetsClient.GetDatasetsAsync())
        {
            datasets.Add(MapToResponse(dataset));
        }

        return Ok(datasets);
    }

    /// <summary>
    /// Lists all versions of a specific dataset.
    /// </summary>
    [HttpGet("{datasetName}/versions")]
    public async Task<IActionResult> ListDatasetVersions(string datasetName)
    {
        var client = _clientFactory.GetClient();
        var datasetsClient = client.Datasets;

        var versions = new List<DatasetResponse>();
        await foreach (var dataset in datasetsClient.GetDatasetVersionsAsync(datasetName))
        {
            versions.Add(MapToResponse(dataset));
        }

        return Ok(versions);
    }

    /// <summary>
    /// Gets a specific version of a dataset.
    /// </summary>
    [HttpGet("{datasetName}/versions/{datasetVersion}")]
    public async Task<IActionResult> GetDatasetVersion(string datasetName, string datasetVersion)
    {
        try
        {
            var client = _clientFactory.GetClient();
            var datasetsClient = client.Datasets;

            var result = await datasetsClient.GetDatasetAsync(datasetName, datasetVersion);
            return Ok(MapToResponse(result.Value));
        }
        catch (ClientResultException ex) when (ex.Status == 404)
        {
            return NotFound(new { error = "Dataset version not found." });
        }
    }

    /// <summary>
    /// Creates or updates a file dataset version in the Foundry project.
    /// A file dataset references a single file in Azure Blob Storage.
    /// </summary>
    [HttpPut("{datasetName}/versions/{datasetVersion}/file")]
    public async Task<IActionResult> CreateOrUpdateFileDataset(
        string datasetName,
        string datasetVersion,
        [FromBody] CreateFileDatasetRequest request)
    {
        var client = _clientFactory.GetClient();
        var datasetsClient = client.Datasets;

        var dataset = new FileDataset(new Uri(request.DataUri))
        {
            ConnectionName = request.ConnectionName,
            Description = request.Description
        };

        var content = BinaryContent.Create(dataset, ModelReaderWriterOptions.Json);
        var result = await datasetsClient.CreateOrUpdateAsync(datasetName, datasetVersion, content);

        _logger.LogInformation("Created/updated file dataset version successfully");

        var created = ModelReaderWriter.Read<AIProjectDataset>(result.GetRawResponse().Content);
        if (created is null)
            return StatusCode(500, new { error = "Failed to deserialize the created dataset from the response." });

        return Ok(MapToResponse(created));
    }

    /// <summary>
    /// Creates or updates a folder dataset version in the Foundry project.
    /// A folder dataset references a folder (prefix) in Azure Blob Storage.
    /// </summary>
    [HttpPut("{datasetName}/versions/{datasetVersion}/folder")]
    public async Task<IActionResult> CreateOrUpdateFolderDataset(
        string datasetName,
        string datasetVersion,
        [FromBody] CreateFolderDatasetRequest request)
    {
        var client = _clientFactory.GetClient();
        var datasetsClient = client.Datasets;

        var dataset = new FolderDataset(new Uri(request.DataUri))
        {
            ConnectionName = request.ConnectionName,
            Description = request.Description
        };

        var content = BinaryContent.Create(dataset, ModelReaderWriterOptions.Json);
        var result = await datasetsClient.CreateOrUpdateAsync(datasetName, datasetVersion, content);

        _logger.LogInformation("Created/updated folder dataset version successfully");

        var created = ModelReaderWriter.Read<AIProjectDataset>(result.GetRawResponse().Content);
        if (created is null)
            return StatusCode(500, new { error = "Failed to deserialize the created dataset from the response." });

        return Ok(MapToResponse(created));
    }

    /// <summary>
    /// Deletes a specific version of a dataset.
    /// </summary>
    [HttpDelete("{datasetName}/versions/{datasetVersion}")]
    public async Task<IActionResult> DeleteDatasetVersion(string datasetName, string datasetVersion)
    {
        var client = _clientFactory.GetClient();
        var datasetsClient = client.Datasets;

        await datasetsClient.DeleteAsync(datasetName, datasetVersion);

        _logger.LogInformation("Deleted dataset version successfully");
        return NoContent();
    }

    private static DatasetResponse MapToResponse(AIProjectDataset dataset) =>
        new()
        {
            Id = dataset.Id,
            Name = dataset.Name,
            Version = dataset.Version,
            Description = dataset.Description,
            Type = dataset switch
            {
                FileDataset => "UriFile",
                FolderDataset => "UriFolder",
                _ => null
            },
            IsReference = dataset.IsReference,
            ConnectionName = dataset.ConnectionName
        };
}
