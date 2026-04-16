# Azure AI Foundry Agent Management API

A .NET 10 Web API for creating and managing agents and model deployments in [Microsoft Azure AI Foundry](https://ai.azure.com). Built using the official [Azure.AI.Projects](https://www.nuget.org/packages/Azure.AI.Projects/) SDK (v2.0.0).

## Features

- **Agent Management** – Create, list, retrieve, and delete prompt agents and hosted agents in Azure AI Foundry
- **Model Deployment Management** – List and retrieve model deployments available in your Foundry project
- **Flexible Authentication** – Supports `DefaultAzureCredential` (for local dev / managed identity) and `ClientSecretCredential` (for service principal-based auth in higher environments)
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
    "ClientSecret": ""
  }
}
```

### Option 2: Environment Variables

Override configuration at runtime using environment variables (useful for CI/CD and containers):

```bash
export AzureFoundry__ProjectEndpoint="https://your-project.services.ai.azure.com"
export AzureFoundry__DefaultModelDeploymentName="gpt-4o"

# For Service Principal auth (higher environments):
export ServicePrincipal__TenantId="<your-tenant-id>"
export ServicePrincipal__ClientId="<your-client-id>"
export ServicePrincipal__ClientSecret="<your-client-secret>"
```

### Authentication

| Scenario | Credential Used | Configuration |
|---|---|---|
| **Local Development** | `DefaultAzureCredential` (uses `az login`, VS, etc.) | Leave `ServicePrincipal` fields empty |
| **Higher Environments** | `ClientSecretCredential` (Service Principal) | Populate `ServicePrincipal` section with Tenant ID, Client ID, and Client Secret |

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

#### Example: Create a Hosted Agent

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

---

## Project Structure

```
AzureAIFoundryApi/
├── Configuration/
│   ├── AzureFoundrySettings.cs       # Foundry project endpoint config
│   └── ServicePrincipalSettings.cs   # Service Principal auth config
├── Controllers/
│   ├── AgentsController.cs           # Agent CRUD endpoints
│   └── DeploymentsController.cs      # Model deployment endpoints
├── Models/
│   ├── AgentModels.cs                # Request/response models for agents
│   └── DeploymentModels.cs           # Response models for deployments
├── Services/
│   ├── AIProjectClientFactory.cs     # Factory for AIProjectClient
│   └── CredentialFactory.cs          # TokenCredential factory (Default / SP)
├── Program.cs                        # App startup and DI configuration
├── appsettings.json                  # Configuration file
└── AzureAIFoundryApi.csproj          # Project file (.NET 10)
```

---

## Key Dependencies

| Package | Version | Purpose |
|---|---|---|
| [Azure.AI.Projects](https://www.nuget.org/packages/Azure.AI.Projects/) | 2.0.0 | Azure AI Foundry SDK – agents, deployments, connections |
| [Azure.Identity](https://www.nuget.org/packages/Azure.Identity/) | 1.21.0 | Azure authentication (DefaultAzureCredential, ClientSecretCredential) |

---

## Notes

- **Hosted Agents** use the `HostedAgentDefinition` which is currently marked as experimental in the SDK (`AAIP001`). This diagnostic is suppressed in the project file.
- The SDK currently supports **reading** model deployments (list/get). Creating new model deployments via the data-plane SDK is not yet supported—model deployment creation is managed through the Azure Portal, Azure CLI, or the Azure Resource Manager (ARM) APIs.
- This project uses the v1 GA data-plane REST APIs of Azure AI Foundry via the `Azure.AI.Projects` SDK.
