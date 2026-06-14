# Deployment Scripts

This directory contains helper scripts to automate the deployment of agents to Azure AI Foundry from source code.

## Available Scripts

### 1. deploy-agent.sh (Bash)

A bash script for deploying agents from source code on Linux/macOS systems.

**Requirements:**
- `bash`
- `zip` utility
- `curl`
- `jq` (optional, for pretty-printing JSON responses)

**Usage:**
```bash
# Set your Azure AI Foundry endpoint
export AZURE_FOUNDRY_ENDPOINT="https://your-project.services.ai.azure.com"

# Deploy an agent
./deploy-agent.sh my-agent ./my-agent-source python_3_13 main.py
```

**Environment Variables:**
- `AZURE_FOUNDRY_ENDPOINT` - **Required**: Your Azure AI Foundry project endpoint
- `FOUNDRY_API_URL` - Optional: API URL (default: https://localhost:5001)
- `AGENT_CPU` - Optional: CPU allocation (default: 2)
- `AGENT_MEMORY` - Optional: Memory allocation (default: 4Gi)

### 2. deploy_agent.py (Python)

A Python script for deploying agents from source code, works on all platforms (Linux, macOS, Windows).

**Requirements:**
- Python 3.8+
- `requests` library: `pip install requests`

**Usage:**
```bash
# Set your Azure AI Foundry endpoint
export AZURE_FOUNDRY_ENDPOINT="https://your-project.services.ai.azure.com"

# Deploy an agent
python deploy_agent.py my-agent ./my-agent-source python_3_13 main.py
```

**Windows PowerShell:**
```powershell
$env:AZURE_FOUNDRY_ENDPOINT="https://your-project.services.ai.azure.com"
python deploy_agent.py my-agent .\my-agent-source python_3_13 main.py
```

**Environment Variables:**
- `AZURE_FOUNDRY_ENDPOINT` - **Required**: Your Azure AI Foundry project endpoint
- `FOUNDRY_API_URL` - Optional: API URL (default: https://localhost:5001)
- `AGENT_CPU` - Optional: CPU allocation (default: 2)
- `AGENT_MEMORY` - Optional: Memory allocation (default: 4Gi)

## Examples

### Python Agent Deployment

```bash
# Set environment
export AZURE_FOUNDRY_ENDPOINT="https://your-project.services.ai.azure.com"

# Deploy Python agent
./deploy-agent.sh customer-service-agent ./agents/customer-service python_3_13 main.py
```

### .NET Agent Deployment

```bash
# Set environment
export AZURE_FOUNDRY_ENDPOINT="https://your-project.services.ai.azure.com"

# Build and publish .NET agent first
cd ./agents/data-processor
dotnet publish -c Release -r linux-x64 -o ./publish

# Deploy from publish directory
cd ../..
./deploy-agent.sh data-processor-agent ./agents/data-processor/publish dotnet_10 DataProcessor.dll
```

### Custom Resource Allocation

```bash
# Deploy with custom CPU and memory
export AGENT_CPU="4"
export AGENT_MEMORY="8Gi"
./deploy-agent.sh high-perf-agent ./agents/high-perf python_3_13 main.py
```

## What These Scripts Do

1. **Package** - Creates a zip file of your source code directory
2. **Encode** - Converts the zip to base64 for API transmission
3. **Detect** - Automatically detects build commands (pip install, dotnet restore)
4. **Deploy** - Posts the request to the Azure AI Foundry API
5. **Verify** - Displays the deployment status and next steps

## Troubleshooting

### "AZURE_FOUNDRY_ENDPOINT is not set"

Set your project endpoint:
```bash
export AZURE_FOUNDRY_ENDPOINT="https://your-project.services.ai.azure.com"
```

Find your endpoint in the Azure AI Foundry portal under your project's overview page.

### "Source directory does not exist"

Ensure the path to your source directory is correct:
```bash
# Use absolute path
./deploy-agent.sh my-agent /full/path/to/source python_3_13 main.py

# Or relative path from current directory
./deploy-agent.sh my-agent ./relative/path/to/source python_3_13 main.py
```

### Connection errors

If the API is running on a different URL or port:
```bash
export FOUNDRY_API_URL="https://your-api-host:8080"
./deploy-agent.sh my-agent ./source python_3_13 main.py
```

### Large source code packages

For very large codebases, consider:
- Excluding unnecessary files (tests, docs, examples)
- Using `.dockerignore` patterns as a guide
- Pre-building dependencies instead of relying on build commands

## Integration with CI/CD

These scripts can be used in CI/CD pipelines:

**GitHub Actions:**
```yaml
- name: Deploy Agent
  env:
    AZURE_FOUNDRY_ENDPOINT: ${{ secrets.AZURE_FOUNDRY_ENDPOINT }}
  run: |
    chmod +x ./scripts/deploy-agent.sh
    ./scripts/deploy-agent.sh my-agent ./agent-source python_3_13 main.py
```

**Azure DevOps:**
```yaml
- script: |
    chmod +x ./scripts/deploy-agent.sh
    ./scripts/deploy-agent.sh my-agent ./agent-source python_3_13 main.py
  env:
    AZURE_FOUNDRY_ENDPOINT: $(AzureFoundryEndpoint)
  displayName: 'Deploy Agent to Foundry'
```

## See Also

- [Complete deployment documentation](../DEPLOY_FROM_SOURCE.md)
- [API documentation](../RUNME.md)
- [Sample payloads](../SamplePayloads/)
