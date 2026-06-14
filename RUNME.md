# Azure AI Foundry Agent Management API

A .NET 10 Web API for creating and managing agents, model deployments, connections, indexes, datasets, and telemetry in [Microsoft Azure AI Foundry](https://ai.azure.com). Built using the official [Azure.AI.Projects](https://www.nuget.org/packages/Azure.AI.Projects/) SDK (v2.0.0).

## Features

- **Agent Management** – Create, list, retrieve, and delete prompt agents and hosted agents in Azure AI Foundry
- **Model Deployment Management** – List and retrieve model deployments available in your Foundry project
- **Connection Management** – List and retrieve connections (Azure OpenAI, Azure AI Search, Blob Storage, etc.) with optional credential retrieval
- **Index Management** – List, get, create, and delete vector search index versions (Azure AI Search, Managed Azure AI Search, Cosmos DB)
- **Dataset Management** – List, get, create, and delete dataset versions (file and folder datasets backed by Azure Blob Storage)
- **Telemetry** – Retrieve the Application Insights connection string for distributed tracing
- **Flexible Authentication** – Supports `DefaultAzureCredential` (for local dev / managed identity), `ClientSecretCredential` (for service principal secret-based auth), and `ClientCertificateCredential` (for service principal certificate-based auth)
- **Customizable Project Endpoint** – Configure the Foundry project endpoint via `appsettings.json` or environment variables

---

## Prerequisites

1. **.NET 10 SDK** – [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
2. **Azure Subscription** with an [Azure AI Foundry](https://ai.azure.com) project created
3. **Azure AI Foundry Project Endpoint** – Found in the Azure AI Foundry portal under your project's overview page (e.g., `https://your-project.services.ai.azure.com`)
4. **Azure CLI** (optional, for `DefaultAzureCredential` local auth) – `az login`

---

## Configuration

### Option 1: `appsettings.json`

Edit `AzureAIFoundryApi/appsettings.json`:

```json
{
  "AzureFoundry": {
    "ProjectEndpoint": "https://your-project.services.ai.azure.com",
    "DefaultModelDeploymentName": "gpt-4o"
  },
  "ServicePrincipal": {
    "TenantId": "",
    "ClientId": "",
    "ClientSecret": "",
    "CertificateThumbprint": "",
    "CertificateFilePath": "",
    "CertificatePassword": ""
  }
}
```

### Option 2: Environment Variables

Override configuration at runtime using environment variables (useful for CI/CD and containers):

```bash
export AzureFoundry__ProjectEndpoint="https://your-project.services.ai.azure.com"
export AzureFoundry__DefaultModelDeploymentName="gpt-4o"

# For Service Principal auth with client secret:
export ServicePrincipal__TenantId="<your-tenant-id>"
export ServicePrincipal__ClientId="<your-client-id>"
export ServicePrincipal__ClientSecret="<your-client-secret>"

# For Service Principal auth with a certificate (thumbprint from cert store):
export ServicePrincipal__TenantId="<your-tenant-id>"
export ServicePrincipal__ClientId="<your-client-id>"
export ServicePrincipal__CertificateThumbprint="<your-certificate-thumbprint>"

# For Service Principal auth with a certificate (PFX file):
export ServicePrincipal__TenantId="<your-tenant-id>"
export ServicePrincipal__ClientId="<your-client-id>"
export ServicePrincipal__CertificateFilePath="/path/to/cert.pfx"
export ServicePrincipal__CertificatePassword="<optional-pfx-password>"
```

### Authentication

| Scenario | Credential Used | Configuration |
|---|---|---|
| **Local Development** | `DefaultAzureCredential` (uses `az login`, VS, etc.) | Leave `ServicePrincipal` fields empty |
| **Client Secret** | `ClientSecretCredential` (Service Principal) | Populate `TenantId`, `ClientId`, and `ClientSecret` |
| **Certificate (store)** | `ClientCertificateCredential` (Service Principal) | Populate `TenantId`, `ClientId`, and `CertificateThumbprint` |
| **Certificate (file)** | `ClientCertificateCredential` (Service Principal) | Populate `TenantId`, `ClientId`, `CertificateFilePath`, and optionally `CertificatePassword` |

Certificate authentication takes priority over client secret when both are configured.
The thumbprint lookup searches `CurrentUser\My` first, then `LocalMachine\My`.

The Service Principal must have the appropriate permissions in Microsoft Entra ID and be assigned the necessary roles on the Azure AI Foundry resource (e.g., `Azure AI Developer` or `Contributor`).

---

## How to Run

### 1. Clone the Repository

```bash
git clone https://github.com/JarvAmrit/DotNet-Agent-FrameworkSamples.git
cd DotNet-Agent-FrameworkSamples
```

### 2. Configure Your Endpoint

Edit `AzureAIFoundryApi/appsettings.json` and set your `ProjectEndpoint`, or set it via environment variable as shown above.

### 3. Authenticate (Local Development)

```bash
az login
```

### 4. Build and Run

```bash
cd AzureAIFoundryApi
dotnet build
dotnet run
```

The API will start on `https://localhost:5001` (or `http://localhost:5000`).

### 5. Access the OpenAPI Docs

When running in Development mode, the OpenAPI (Swagger) spec is available at:

```
https://localhost:5001/openapi/v1.json
```

---

## API Endpoints

### Agents

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/agents` | List all agents (optional `?kind=prompt\|hosted\|workflow`) |
| `GET` | `/api/agents/{agentName}` | Get a specific agent by name |
| `GET` | `/api/agents/{agentName}/versions` | List all versions of an agent |
| `GET` | `/api/agents/{agentName}/versions/{version}` | Get a specific agent version |
| `POST` | `/api/agents/prompt` | Create a new prompt (declarative) agent |
| `POST` | `/api/agents/hosted` | Create a new hosted (containerized) agent |
| `POST` | `/api/agents/hosted/from-source` | **NEW**: Create a hosted agent from source code (zip file) |
| `POST` | `/api/agents/{agentName}/invoke` | **Invoke** an agent — send a message and get the response |
| `DELETE` | `/api/agents/{agentName}` | Delete an agent and all its versions |
| `DELETE` | `/api/agents/{agentName}/versions/{version}` | Delete a specific agent version |

#### Example: Create a Prompt Agent

```bash
curl -X POST https://localhost:5001/api/agents/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "agentName": "math-tutor",
    "model": "gpt-4o",
    "instructions": "You are a helpful math tutor. Help students solve problems step by step.",
    "description": "A math tutoring agent"
  }'
```

#### Example: Create a Hosted Agent from Source Code

**NEW**: Deploy agents directly from source code without building container images.

```bash
# First, package your agent code and convert to base64
cd my-agent-source/
zip -r agent.zip . -x "*.pyc" -x "__pycache__/*"
base64 -w 0 agent.zip > agent-base64.txt

# Deploy using the API
curl -X POST 'https://localhost:5001/api/agents/hosted/from-source?projectEndpoint=https://your-project.services.ai.azure.com' \
  -H "Content-Type: application/json" \
  -d '{
    "agentName": "my-python-agent",
    "sourceCodeZipBase64": "<paste-content-from-agent-base64.txt>",
    "runtime": "python_3_13",
    "entryPoint": "main.py",
    "cpu": "2",
    "memory": "4Gi",
    "protocolVersions": [
      { "protocol": "A2A", "version": "0.2.1" }
    ],
    "environmentVariables": {
      "LOG_LEVEL": "info"
    },
    "buildCommand": "pip install -r requirements.txt",
    "description": "Python agent deployed from source"
  }'
```

**For comprehensive documentation, automation scripts, and complete examples, see [DEPLOY_FROM_SOURCE.md](./DEPLOY_FROM_SOURCE.md).**

#### Example: Create a Hosted Agent (from Container Image)

```bash
curl -X POST https://localhost:5001/api/agents/hosted \
  -H "Content-Type: application/json" \
  -d '{
    "agentName": "my-hosted-agent",
    "image": "myregistry.azurecr.io/my-agent:latest",
    "cpu": "1",
    "memory": "2Gi",
    "protocolVersions": [
      { "protocol": "A2A", "version": "0.2.1" }
    ],
    "environmentVariables": {
      "MY_VAR": "my-value"
    },
    "description": "A custom hosted agent"
  }'

#### Example: Invoke an Agent

```bash
# Start a new conversation
# Start a new conversation
curl -X POST https://localhost:5001/api/agents/math-tutor/invoke \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Can you help me solve: 3x + 7 = 22?"
  }'

# Continue the same conversation using the responseId from the previous response
curl -X POST https://localhost:5001/api/agents/math-tutor/invoke \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What about 5x - 3 = 17?",
    "previousResponseId": "<responseId from previous response>"
  }'
```

Optional query parameter: `?conversationId=<id>` links the call to a Foundry conversation tracking record.
```

### Deployments (Model Management)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/deployments` | List all model deployments (optional `?modelPublisher=...&modelName=...&deploymentType=...`) |
| `GET` | `/api/deployments/{deploymentName}` | Get details of a specific deployment |

#### Example: List Deployments

```bash
curl https://localhost:5001/api/deployments
```

#### Example: List Deployments by Publisher

```bash
curl "https://localhost:5001/api/deployments?modelPublisher=OpenAI"
```

### Connections

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/connections` | List all connections (optional `?type=...&defaultOnly=true`) |
| `GET` | `/api/connections/default` | Get the default connection (optional `?type=...&includeCredentials=true`) |
| `GET` | `/api/connections/{connectionName}` | Get a specific connection (optional `?includeCredentials=true`) |

Supported `type` values: `AzureOpenAI`, `AzureAISearch`, `AzureBlobStorage`, `AzureStorageAccount`, `CosmosDB`, `APIKey`, `ApplicationInsights`, `Custom`, `RemoteTool`.

#### Example: List All Connections

```bash
curl https://localhost:5001/api/connections
```

#### Example: Get the Default Azure OpenAI Connection with Credentials

```bash
curl "https://localhost:5001/api/connections/default?type=AzureOpenAI&includeCredentials=true"
```

#### Example: Get a Named Connection

```bash
curl "https://localhost:5001/api/connections/my-openai-connection?includeCredentials=true"
```

### Indexes

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/indexes` | List all indexes |
| `GET` | `/api/indexes/{indexName}/versions` | List all versions of an index |
| `GET` | `/api/indexes/{indexName}/versions/{indexVersion}` | Get a specific index version |
| `PUT` | `/api/indexes/{indexName}/versions/{indexVersion}/azure-search` | Create or update an Azure AI Search index version |
| `PUT` | `/api/indexes/{indexName}/versions/{indexVersion}/managed` | Create or update a Managed Azure AI Search index version |
| `DELETE` | `/api/indexes/{indexName}/versions/{indexVersion}` | Delete a specific index version |

#### Example: Create an Azure AI Search Index

```bash
curl -X PUT https://localhost:5001/api/indexes/my-index/versions/1/azure-search \
  -H "Content-Type: application/json" \
  -d '{
    "connectionName": "my-search-connection",
    "indexName": "my-azure-search-index",
    "description": "Product catalog search index"
  }'
```

#### Example: Create a Managed Azure AI Search Index

```bash
curl -X PUT https://localhost:5001/api/indexes/my-managed-index/versions/1/managed \
  -H "Content-Type: application/json" \
  -d '{
    "vectorStoreId": "vs_abc123",
    "description": "Managed vector store index"
  }'
```

### Datasets

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/datasets` | List all datasets |
| `GET` | `/api/datasets/{datasetName}/versions` | List all versions of a dataset |
| `GET` | `/api/datasets/{datasetName}/versions/{datasetVersion}` | Get a specific dataset version |
| `PUT` | `/api/datasets/{datasetName}/versions/{datasetVersion}/file` | Create or update a file dataset version |
| `PUT` | `/api/datasets/{datasetName}/versions/{datasetVersion}/folder` | Create or update a folder dataset version |
| `DELETE` | `/api/datasets/{datasetName}/versions/{datasetVersion}` | Delete a specific dataset version |

#### Example: Create a File Dataset

```bash
curl -X PUT https://localhost:5001/api/datasets/my-dataset/versions/1/file \
  -H "Content-Type: application/json" \
  -d '{
    "dataUri": "https://mystorageaccount.blob.core.windows.net/mycontainer/data.jsonl",
    "connectionName": "my-storage-connection",
    "description": "Training dataset for fine-tuning"
  }'
```

#### Example: Create a Folder Dataset

```bash
curl -X PUT https://localhost:5001/api/datasets/my-dataset/versions/1/folder \
  -H "Content-Type: application/json" \
  -d '{
    "dataUri": "https://mystorageaccount.blob.core.windows.net/mycontainer/data/",
    "connectionName": "my-storage-connection",
    "description": "Folder of evaluation files"
  }'
```

### Telemetry

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/telemetry/app-insights` | Get the Application Insights connection string |

#### Example: Get Application Insights Connection String

```bash
curl https://localhost:5001/api/telemetry/app-insights
```

Use the returned connection string to configure your OpenTelemetry SDK:

```csharp
using Azure.Monitor.OpenTelemetry.Exporter;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAzureMonitorTraceExporter(options =>
            options.ConnectionString = "<connection-string-from-api>"));
```

### Health

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/health` | Overall API and Foundry connectivity health |
| `GET` | `/api/health/agents/{agentName}` | Per-agent health — catalog + runtime availability |

The health endpoints are designed for monitoring dashboards, alerting, and operational audits.

#### Overall Health

Returns `HTTP 200` (Healthy) or `HTTP 503` (Unhealthy) and a structured JSON payload describing the Foundry connectivity check.

```bash
curl https://localhost:5001/api/health
```

Example response:

```json
{
  "status": "Healthy",
  "timestamp": "2024-06-01T10:00:00+00:00",
  "checks": [
    {
      "name": "FoundryConnectivity",
      "status": "Healthy",
      "description": "Successfully connected to the Azure AI Foundry project endpoint.",
      "error": null
    }
  ]
}
```

#### Per-Agent Health

Checks two dimensions for a given agent and aggregates the result:

| Check | What it verifies |
|---|---|
| `AgentCatalogAvailability` | Agent is registered in the Foundry catalog (`AgentAdministrationClient`) |
| `AgentLatestVersionAvailability` | Agent has at least one deployed version in the catalog |

Status values: `Healthy` → `Degraded` → `Unhealthy`. Returns `HTTP 200` for Healthy/Degraded, `HTTP 503` for Unhealthy.

```bash
curl https://localhost:5001/api/health/agents/math-tutor
```

Example response:

```json
{
  "status": "Healthy",
  "timestamp": "2024-06-01T10:00:00+00:00",
  "agentName": "math-tutor",
  "agentId": "agent_abc123",
  "checks": [
    {
      "name": "AgentCatalogAvailability",
      "status": "Healthy",
      "description": "Agent 'math-tutor' is registered in the Foundry catalog (id: agent_abc123).",
      "error": null
    },
    {
      "name": "AgentLatestVersionAvailability",
      "status": "Healthy",
      "description": "Agent 'math-tutor' has at least one deployed version.",
      "error": null
    }
  ]
}
```

---

## Sample Payloads

The `SamplePayloads/` folder at the root of the repository contains ready-to-use JSON request bodies and response examples for every endpoint. Use them for manual testing, Postman imports, or documentation.

```
SamplePayloads/
├── Agents/
│   ├── POST_prompt.json          — POST /api/agents/prompt
│   ├── POST_hosted.json          — POST /api/agents/hosted
│   ├── POST_invoke.json          — POST /api/agents/{agentName}/invoke  (new conversation)
│   └── POST_invoke_continue.json — POST /api/agents/{agentName}/invoke  (continue conversation)
├── Connections/
│   └── GET_list.json             — GET /api/connections  (query param examples)
├── Deployments/
│   └── GET_list.json             — GET /api/deployments  (query param examples)
├── Indexes/
│   ├── PUT_azure-search.json      — PUT /api/indexes/{name}/versions/{v}/azure-search
│   └── PUT_managed.json           — PUT /api/indexes/{name}/versions/{v}/managed
├── Datasets/
│   ├── PUT_file.json              — PUT /api/datasets/{name}/versions/{v}/file
│   └── PUT_folder.json            — PUT /api/datasets/{name}/versions/{v}/folder
└── Health/
    ├── GET_overall.json           — Response example: GET /api/health
    └── GET_agent.json             — Response example: GET /api/health/agents/{agentName}
```

---

## Project Structure

```
AzureAIFoundryApi/
├── Configuration/
│   ├── AzureFoundrySettings.cs       # Foundry project endpoint config
│   └── ServicePrincipalSettings.cs   # Service Principal auth config (secret & certificate)
├── Controllers/
│   ├── AgentsController.cs           # Agent CRUD + invoke endpoints
│   ├── ConnectionsController.cs      # Connection list/get endpoints
│   ├── DatasetsController.cs         # Dataset CRUD endpoints
│   ├── DeploymentsController.cs      # Model deployment endpoints
│   ├── HealthController.cs           # Health check endpoints (overall + per-agent)
│   ├── IndexesController.cs          # Index CRUD endpoints
│   └── TelemetryController.cs        # Application Insights telemetry endpoint
├── Models/
│   ├── AgentModels.cs                # Request/response models for agents
│   ├── ConnectionModels.cs           # Response models for connections
│   ├── DatasetModels.cs              # Request/response models for datasets
│   ├── DeploymentModels.cs           # Response models for deployments
│   ├── HealthModels.cs               # Models for health check responses
│   ├── IndexModels.cs                # Request/response models for indexes
│   ├── InvokeModels.cs               # Request/response models for agent invoke
│   └── TelemetryModels.cs            # Response models for telemetry
├── Services/
│   ├── AIProjectClientFactory.cs     # Factory for AIProjectClient
│   └── CredentialFactory.cs          # TokenCredential factory (Default / Secret / Certificate)
├── Program.cs                        # App startup and DI configuration
├── appsettings.json                  # Configuration file
└── AzureAIFoundryApi.csproj          # Project file (.NET 10)
```

---

## Key Dependencies

| Package | Version | Purpose |
|---|---|---|
| [Azure.AI.Projects](https://www.nuget.org/packages/Azure.AI.Projects/) | 2.0.0 | Azure AI Foundry SDK – agents, deployments, connections, indexes, datasets, telemetry |
| [Azure.Identity](https://www.nuget.org/packages/Azure.Identity/) | 1.21.0 | Azure authentication (DefaultAzureCredential, ClientSecretCredential, ClientCertificateCredential) |

---

## Notes

- **Hosted Agents** use the `HostedAgentDefinition` which is currently marked as experimental in the SDK (`AAIP001`). This diagnostic is suppressed in the project file.
- The SDK currently supports **reading** model deployments (list/get). Creating new model deployments via the data-plane SDK is not yet supported—model deployment creation is managed through the Azure Portal, Azure CLI, or the Azure Resource Manager (ARM) APIs.
- **Dataset creation** uses the protocol-layer `CreateOrUpdate` with `BinaryContent` serialization, as no strongly-typed convenience overload is available in the current SDK version.
- This project uses the v1 GA data-plane REST APIs of Azure AI Foundry via the `Azure.AI.Projects` SDK.
