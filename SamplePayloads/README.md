# Sample Payloads

Each subfolder contains `.json` files with sample request bodies (and example response shapes) for every API endpoint. Files whose content is a query-only operation include a `queryParams` object and `exampleUrl` for clarity since they carry no request body.

## Structure

| Folder | Endpoints covered |
|---|---|
| `agents/` | `POST /api/agents/prompt`, `POST /api/agents/hosted`, `POST /api/agents/{name}/invoke` |
| `agent-threads/` | `POST /api/agent-threads/{id}/messages`, `POST /api/agent-threads/{id}/runs` |
| `connections/` | `GET /api/connections`, `GET /api/connections/default`, `GET /api/connections/{name}` |
| `datasets/` | `PUT /api/datasets/{name}/versions/{v}/file`, `PUT /api/datasets/{name}/versions/{v}/folder` |
| `deployments/` | `GET /api/deployments`, `GET /api/deployments/{name}` |
| `health/` | `GET /api/health` — sample healthy and degraded response shapes |
| `indexes/` | `PUT /api/indexes/{name}/versions/{v}/azure-search`, `PUT /api/indexes/{name}/versions/{v}/managed` |
| `telemetry/` | `GET /api/telemetry/app-insights` — sample response shape |

## Usage with curl

```bash
BASE=https://localhost:5001

# Invoke an agent (one-shot)
curl -k -X POST "$BASE/api/agents/math-tutor/invoke" \
  -H "Content-Type: application/json" \
  -d @agents/invoke-agent.json

# Check overall health
curl -k "$BASE/api/health"

# Check agents subsystem health
curl -k "$BASE/api/health/agents"

# Create a thread
curl -k -X POST "$BASE/api/agent-threads"

# Add a message to a thread
curl -k -X POST "$BASE/api/agent-threads/{threadId}/messages" \
  -H "Content-Type: application/json" \
  -d @agent-threads/create-thread-message.json

# Start a run on a thread
curl -k -X POST "$BASE/api/agent-threads/{threadId}/runs" \
  -H "Content-Type: application/json" \
  -d @agent-threads/create-run.json

# Poll run status
curl -k "$BASE/api/agent-threads/{threadId}/runs/{runId}"

# List messages after run completes
curl -k "$BASE/api/agent-threads/{threadId}/messages"
```
