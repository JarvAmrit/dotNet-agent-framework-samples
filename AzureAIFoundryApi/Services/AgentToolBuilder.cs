using Azure.AI.Projects.Agents;
using AzureAIFoundryApi.Models;
using OpenAI.Responses;

namespace AzureAIFoundryApi.Services;

/// <summary>
/// Builds SDK <see cref="ResponseTool"/> instances from declarative <see cref="AgentToolConfig"/>
/// entries so that callers can attach any supported tool to a prompt agent through
/// configuration alone — no code change required when adding new tool instances.
/// </summary>
public static class AgentToolBuilder
{
    /// <summary>
    /// Converts a single <see cref="AgentToolConfig"/> to the corresponding SDK
    /// <see cref="ResponseTool"/> that can be added to
    /// <c>DeclarativeAgentDefinition.Tools</c>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when required properties are missing or the <c>Type</c> is unsupported.
    /// </exception>
    public static ResponseTool Build(AgentToolConfig config) =>
        config.Type.ToUpperInvariant() switch
        {
            "AZUREAISEARCH" => BuildAzureAISearch(config),
            "BINGGROUNDING" => BuildBingGrounding(config),
            "BINGCUSTOMSEARCH" => BuildBingCustomSearch(config),
            "OPENAPI" => BuildOpenAPI(config),
            _ => throw new ArgumentException(
                $"Unsupported tool type '{config.Type}'. " +
                "Supported types: AzureAISearch, BingGrounding, BingCustomSearch, OpenAPI.")
        };

    // ── AzureAISearch ─────────────────────────────────────────────────────────

    private static ResponseTool BuildAzureAISearch(AgentToolConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ConnectionName))
            throw new ArgumentException("AzureAISearch tool requires 'connectionName'.");
        if (string.IsNullOrWhiteSpace(config.IndexName))
            throw new ArgumentException("AzureAISearch tool requires 'indexName'.");

        var index = new AzureAISearchToolIndex
        {
            ProjectConnectionId = config.ConnectionName,
            IndexName = config.IndexName,
            TopK = config.TopK
        };

        if (!string.IsNullOrWhiteSpace(config.QueryType))
            index.QueryType = new AzureAISearchQueryType(config.QueryType);

        return (ResponseTool)new AzureAISearchTool(new AzureAISearchToolOptions([index]));
    }

    // ── BingGrounding ─────────────────────────────────────────────────────────

    private static ResponseTool BuildBingGrounding(AgentToolConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ConnectionName))
            throw new ArgumentException("BingGrounding tool requires 'connectionName'.");

        var searchConfig = new BingGroundingSearchConfiguration(config.ConnectionName)
        {
            Market = config.Market,
            Count = config.Count,
            Freshness = config.Freshness
        };

        return (ResponseTool)new BingGroundingTool(new BingGroundingSearchToolOptions([searchConfig]));
    }

    // ── BingCustomSearch ──────────────────────────────────────────────────────

    private static ResponseTool BuildBingCustomSearch(AgentToolConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ConnectionName))
            throw new ArgumentException("BingCustomSearch tool requires 'connectionName'.");
        if (string.IsNullOrWhiteSpace(config.InstanceName))
            throw new ArgumentException("BingCustomSearch tool requires 'instanceName'.");

        var searchConfig = new BingCustomSearchConfiguration(
            projectConnectionId: config.ConnectionName,
            instanceName: config.InstanceName)
        {
            Market = config.Market,
            Count = config.Count,
            Freshness = config.Freshness
        };

        return (ResponseTool)new BingCustomSearchPreviewTool(new BingCustomSearchToolOptions([searchConfig]));
    }

    // ── OpenAPI ───────────────────────────────────────────────────────────────

    private static ResponseTool BuildOpenAPI(AgentToolConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
            throw new ArgumentException("OpenAPI tool requires 'name'.");
        if (string.IsNullOrWhiteSpace(config.Spec))
            throw new ArgumentException("OpenAPI tool requires 'spec'.");

        var auth = BuildOpenApiAuth(config);
        var funcDef = new OpenApiFunctionDefinition(
            name: config.Name,
            specificationBytes: BinaryData.FromString(config.Spec),
            authentication: auth);

        return (ResponseTool)new OpenAPITool(funcDef);
    }

    private static OpenApiAuthenticationDetails BuildOpenApiAuth(AgentToolConfig config)
    {
        var authType = (config.AuthType ?? "Connection").ToUpperInvariant();
        return authType switch
        {
            "CONNECTION" => BuildConnectionAuth(config),
            "MANAGED" => BuildManagedAuth(config),
            "ANONYMOUS" => new OpenAPIAnonymousAuthenticationDetails(),
            _ => throw new ArgumentException(
                $"Unsupported OpenAPI authType '{config.AuthType}'. " +
                "Supported values: Connection, Managed, Anonymous.")
        };
    }

    private static OpenApiProjectConnectionAuthenticationDetails BuildConnectionAuth(AgentToolConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ConnectionName))
            throw new ArgumentException(
                "OpenAPI tool with authType 'Connection' requires 'connectionName'.");

        return new OpenApiProjectConnectionAuthenticationDetails(
            new OpenApiProjectConnectionSecurityScheme(config.ConnectionName));
    }

    private static OpenAPIManagedAuthenticationDetails BuildManagedAuth(AgentToolConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Audience))
            throw new ArgumentException(
                "OpenAPI tool with authType 'Managed' requires 'audience'.");

        return new OpenAPIManagedAuthenticationDetails(
            new OpenAPIManagedSecurityScheme(config.Audience));
    }
}
