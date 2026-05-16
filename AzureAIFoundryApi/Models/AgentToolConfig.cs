using System.ComponentModel.DataAnnotations;

namespace AzureAIFoundryApi.Models;

/// <summary>
/// Configuration for a single tool to attach to a prompt agent.
/// Set <see cref="Type"/> to one of the supported values and fill in the
/// properties that apply to that type. Unsupported properties are ignored.
/// </summary>
public class AgentToolConfig
{
    /// <summary>
    /// The tool type. Supported values (case-insensitive):
    /// <list type="bullet">
    ///   <item><term>AzureAISearch</term><description>Azure AI Search grounding tool. Requires <see cref="ConnectionName"/> and <see cref="IndexName"/>.</description></item>
    ///   <item><term>BingGrounding</term><description>Bing web-search grounding tool. Requires <see cref="ConnectionName"/>.</description></item>
    ///   <item><term>BingCustomSearch</term><description>Bing Custom Search preview tool. Requires <see cref="ConnectionName"/> and <see cref="InstanceName"/>.</description></item>
    ///   <item><term>OpenAPI</term><description>OpenAPI (REST) tool. Requires <see cref="Name"/> and <see cref="Spec"/>.</description></item>
    /// </list>
    /// </summary>
    [Required]
    public required string Type { get; set; }

    // ── Shared ────────────────────────────────────────────────────────────────

    /// <summary>
    /// The name of the Foundry project connection that backs this tool.
    /// Required for <c>AzureAISearch</c>, <c>BingGrounding</c>, <c>BingCustomSearch</c>,
    /// and <c>OpenAPI</c> when <see cref="AuthType"/> is <c>Connection</c> (the default).
    /// Must match a connection name listed by <c>GET /api/connections</c>.
    /// </summary>
    public string? ConnectionName { get; set; }

    // ── AzureAISearch ─────────────────────────────────────────────────────────

    /// <summary>
    /// The name of the Azure AI Search index to query.
    /// Required for <c>AzureAISearch</c>.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// The query type for Azure AI Search.
    /// Optional. Supported values: <c>Simple</c>, <c>Semantic</c>, <c>Full</c>,
    /// <c>VectorSimpleHybrid</c>, <c>VectorSemanticHybrid</c>.
    /// </summary>
    public string? QueryType { get; set; }

    /// <summary>
    /// Number of top search results to return. Optional.
    /// </summary>
    public int? TopK { get; set; }

    // ── BingGrounding / BingCustomSearch ──────────────────────────────────────

    /// <summary>
    /// The Bing Custom Search instance name.
    /// Required for <c>BingCustomSearch</c>.
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// The market code for Bing search (e.g. <c>en-US</c>). Optional.
    /// Applies to <c>BingGrounding</c> and <c>BingCustomSearch</c>.
    /// </summary>
    public string? Market { get; set; }

    /// <summary>
    /// Maximum number of Bing search results. Optional.
    /// Applies to <c>BingGrounding</c> and <c>BingCustomSearch</c>.
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// Recency filter for Bing results (e.g. <c>Day</c>, <c>Week</c>, <c>Month</c>). Optional.
    /// Applies to <c>BingGrounding</c> and <c>BingCustomSearch</c>.
    /// </summary>
    public string? Freshness { get; set; }

    // ── OpenAPI ───────────────────────────────────────────────────────────────

    /// <summary>
    /// A short identifier for the OpenAPI tool function (e.g. <c>BlobStorageApi</c>).
    /// Required for <c>OpenAPI</c>.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The OpenAPI 3.x specification as a JSON string.
    /// Required for <c>OpenAPI</c>.
    /// Only include the operations you want the agent to be able to call.
    /// </summary>
    public string? Spec { get; set; }

    /// <summary>
    /// Authentication type for an <c>OpenAPI</c> tool.
    /// Supported values (case-insensitive):
    /// <list type="bullet">
    ///   <item><term>Connection</term><description>Uses a Foundry project connection identified by <see cref="ConnectionName"/>. This is the default.</description></item>
    ///   <item><term>Managed</term><description>Uses a managed identity. Requires <see cref="Audience"/>.</description></item>
    ///   <item><term>Anonymous</term><description>No authentication.</description></item>
    /// </list>
    /// </summary>
    public string? AuthType { get; set; }

    /// <summary>
    /// The token audience URI used when <see cref="AuthType"/> is <c>Managed</c>.
    /// </summary>
    public string? Audience { get; set; }
}
