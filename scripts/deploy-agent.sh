#!/bin/bash
# deploy-agent.sh
# Helper script to deploy agents to Azure AI Foundry from source code
#
# Usage:
#   ./deploy-agent.sh <agent-name> <source-directory> <runtime> <entry-point>
#
# Example:
#   ./deploy-agent.sh my-python-agent ./my-agent python_3_13 main.py
#
# Environment Variables:
#   AZURE_FOUNDRY_ENDPOINT - Your Azure AI Foundry project endpoint (required)
#   FOUNDRY_API_URL - The API URL (default: https://localhost:5001)
#   AGENT_CPU - CPU allocation (default: 2)
#   AGENT_MEMORY - Memory allocation (default: 4Gi)

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
AGENT_NAME="$1"
SOURCE_DIR="$2"
RUNTIME="$3"
ENTRY_POINT="$4"
PROJECT_ENDPOINT="${AZURE_FOUNDRY_ENDPOINT}"
API_URL="${FOUNDRY_API_URL:-https://localhost:5001}"
CPU="${AGENT_CPU:-2}"
MEMORY="${AGENT_MEMORY:-4Gi}"

# Validate arguments
if [ -z "$AGENT_NAME" ] || [ -z "$SOURCE_DIR" ] || [ -z "$RUNTIME" ] || [ -z "$ENTRY_POINT" ]; then
    echo -e "${RED}Error: Missing required arguments${NC}"
    echo ""
    echo "Usage: $0 <agent-name> <source-dir> <runtime> <entry-point>"
    echo ""
    echo "Arguments:"
    echo "  agent-name     - Name for the agent in Azure AI Foundry"
    echo "  source-dir     - Path to the agent source code directory"
    echo "  runtime        - Runtime environment (python_3_13, python_3_14, dotnet_10)"
    echo "  entry-point    - Entry point file (e.g., main.py, MyAgent.dll)"
    echo ""
    echo "Examples:"
    echo "  $0 my-agent ./src python_3_13 main.py"
    echo "  $0 data-processor ./agent dotnet_10 DataProcessor.dll"
    echo ""
    echo "Required Environment Variables:"
    echo "  AZURE_FOUNDRY_ENDPOINT - Your project endpoint (e.g., https://your-project.services.ai.azure.com)"
    echo ""
    echo "Optional Environment Variables:"
    echo "  FOUNDRY_API_URL  - API URL (default: https://localhost:5001)"
    echo "  AGENT_CPU        - CPU allocation (default: 2)"
    echo "  AGENT_MEMORY     - Memory allocation (default: 4Gi)"
    exit 1
fi

if [ -z "$PROJECT_ENDPOINT" ]; then
    echo -e "${RED}Error: AZURE_FOUNDRY_ENDPOINT environment variable is not set${NC}"
    echo "Set it with: export AZURE_FOUNDRY_ENDPOINT='https://your-project.services.ai.azure.com'"
    exit 1
fi

if [ ! -d "$SOURCE_DIR" ]; then
    echo -e "${RED}Error: Source directory '$SOURCE_DIR' does not exist${NC}"
    exit 1
fi

# Create temporary directory for build artifacts
TMP_DIR=$(mktemp -d)
trap "rm -rf $TMP_DIR" EXIT

echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}  Azure AI Foundry - Agent Deployment from Source${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "Agent Name:       ${GREEN}$AGENT_NAME${NC}"
echo -e "Source Directory: ${GREEN}$SOURCE_DIR${NC}"
echo -e "Runtime:          ${GREEN}$RUNTIME${NC}"
echo -e "Entry Point:      ${GREEN}$ENTRY_POINT${NC}"
echo -e "CPU:              ${GREEN}$CPU${NC}"
echo -e "Memory:           ${GREEN}$MEMORY${NC}"
echo -e "Project Endpoint: ${GREEN}$PROJECT_ENDPOINT${NC}"
echo -e "API URL:          ${GREEN}$API_URL${NC}"
echo ""

# Step 1: Package the source code
echo -e "${YELLOW}[1/4] Packaging source code...${NC}"
ZIP_FILE="$TMP_DIR/$AGENT_NAME.zip"

cd "$SOURCE_DIR"
if [ -f "requirements.txt" ]; then
    echo "  ✓ Found requirements.txt"
elif compgen -G "*.csproj" > /dev/null 2>&1; then
    echo "  ✓ Found .NET project file"
fi

zip -r "$ZIP_FILE" . \
    -x "*.pyc" \
    -x "__pycache__/*" \
    -x "*.git/*" \
    -x "node_modules/*" \
    -x ".env" \
    -x ".venv/*" \
    -x "venv/*" \
    -x "bin/*" \
    -x "obj/*" \
    > /dev/null 2>&1

FILE_SIZE=$(du -h "$ZIP_FILE" | cut -f1)
echo -e "  ✓ Created package: ${GREEN}$FILE_SIZE${NC}"

# Step 2: Encode to base64
echo -e "${YELLOW}[2/4] Encoding to base64...${NC}"
if command -v base64 &> /dev/null; then
    # Try base64 with -w 0 (Linux)
    BASE64_CONTENT=$(base64 -w 0 "$ZIP_FILE" 2>/dev/null || base64 "$ZIP_FILE" | tr -d '\n')
else
    echo -e "${RED}Error: base64 command not found${NC}"
    exit 1
fi

BASE64_SIZE=${#BASE64_CONTENT}
echo -e "  ✓ Encoded ${GREEN}$BASE64_SIZE bytes${NC}"

# Step 3: Prepare deployment request
echo -e "${YELLOW}[3/4] Preparing deployment request...${NC}"

# Detect build command based on runtime
BUILD_COMMAND=""
if [ "$RUNTIME" = "python_3_13" ] || [ "$RUNTIME" = "python_3_14" ]; then
    if [ -f "$SOURCE_DIR/requirements.txt" ]; then
        BUILD_COMMAND="pip install -r requirements.txt"
    fi
elif [ "$RUNTIME" = "dotnet_10" ]; then
    BUILD_COMMAND="dotnet restore"
fi

# Create JSON payload
DEPLOY_JSON="$TMP_DIR/deploy-request.json"
cat > "$DEPLOY_JSON" <<EOF
{
  "agentName": "$AGENT_NAME",
  "sourceCodeZipBase64": "$BASE64_CONTENT",
  "runtime": "$RUNTIME",
  "entryPoint": "$ENTRY_POINT",
  "cpu": "$CPU",
  "memory": "$MEMORY",
  "protocolVersions": [
    { "protocol": "A2A", "version": "0.2.1" }
  ],
  "environmentVariables": {
    "DEPLOYED_BY": "deploy-agent-script",
    "DEPLOYED_AT": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  },
EOF

if [ -n "$BUILD_COMMAND" ]; then
cat >> "$DEPLOY_JSON" <<EOF
  "buildCommand": "$BUILD_COMMAND",
EOF
fi

cat >> "$DEPLOY_JSON" <<EOF
  "description": "Deployed from source: $SOURCE_DIR"
}
EOF

echo "  ✓ Request prepared"

# Step 4: Deploy to Azure AI Foundry
echo -e "${YELLOW}[4/4] Deploying to Azure AI Foundry...${NC}"

DEPLOY_URL="$API_URL/api/agents/hosted/from-source?projectEndpoint=$(echo "$PROJECT_ENDPOINT" | jq -sRr @uri)"

HTTP_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$DEPLOY_URL" \
    -H "Content-Type: application/json" \
    -d @"$DEPLOY_JSON")

HTTP_BODY=$(echo "$HTTP_RESPONSE" | head -n -1)
HTTP_CODE=$(echo "$HTTP_RESPONSE" | tail -n 1)

if [ "$HTTP_CODE" -ge 200 ] && [ "$HTTP_CODE" -lt 300 ]; then
    echo -e "${GREEN}✓ Deployment successful!${NC}"
    echo ""
    echo "Response:"
    echo "$HTTP_BODY" | jq '.' 2>/dev/null || echo "$HTTP_BODY"
    echo ""
    echo -e "${GREEN}═══════════════════════════════════════════════════════════${NC}"
    echo -e "${GREEN}  Agent '$AGENT_NAME' has been deployed successfully!${NC}"
    echo -e "${GREEN}═══════════════════════════════════════════════════════════${NC}"
    echo ""
    echo "Next steps:"
    echo "  1. Check agent status:"
    echo "     curl '$API_URL/api/agents/$AGENT_NAME?projectEndpoint=$PROJECT_ENDPOINT'"
    echo ""
    echo "  2. Invoke the agent:"
    echo "     curl -X POST '$API_URL/api/agents/$AGENT_NAME/invoke?projectEndpoint=$PROJECT_ENDPOINT' \\"
    echo "       -H 'Content-Type: application/json' \\"
    echo "       -d '{\"message\": \"Hello, agent!\"}'"
    echo ""
    exit 0
else
    echo -e "${RED}✗ Deployment failed${NC}"
    echo ""
    echo "HTTP Status Code: $HTTP_CODE"
    echo "Response:"
    echo "$HTTP_BODY" | jq '.' 2>/dev/null || echo "$HTTP_BODY"
    echo ""
    exit 1
fi
