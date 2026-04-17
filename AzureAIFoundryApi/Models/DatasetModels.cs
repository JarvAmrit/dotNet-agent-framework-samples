using System.ComponentModel.DataAnnotations;

namespace AzureAIFoundryApi.Models;

/// <summary>
/// Response model representing a dataset version in Azure AI Foundry.
/// </summary>
public class DatasetResponse
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// The dataset type: "UriFile" or "UriFolder".
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Whether the dataset is a reference (data is not deleted when the version is deleted).
    /// </summary>
    public bool? IsReference { get; set; }

    /// <summary>
    /// The Azure Storage Account connection name.
    /// </summary>
    public string? ConnectionName { get; set; }
}

/// <summary>
/// Request model for creating or updating a file dataset version.
/// </summary>
public class CreateFileDatasetRequest
{
    /// <summary>
    /// The URI of the data file (e.g., an Azure Blob Storage URI).
    /// </summary>
    [Required]
    public required string DataUri { get; set; }

    /// <summary>
    /// The Azure Storage Account connection name. Required if not using a pending upload.
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Optional description for the dataset version.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request model for creating or updating a folder dataset version.
/// </summary>
public class CreateFolderDatasetRequest
{
    /// <summary>
    /// The URI of the data folder (e.g., an Azure Blob Storage container or folder URI).
    /// </summary>
    [Required]
    public required string DataUri { get; set; }

    /// <summary>
    /// The Azure Storage Account connection name. Required if not using a pending upload.
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Optional description for the dataset version.
    /// </summary>
    public string? Description { get; set; }
}
