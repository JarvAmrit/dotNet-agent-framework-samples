# Sample Payloads

This folder contains sample request payloads and usage examples for every endpoint exposed by the **AzureAIFoundryApi**.  
Use these files as quick-start templates when testing the API with tools like [httpie](https://httpie.io/), [curl](https://curl.se/), or the Swagger UI (`/openapi/v1.json`).

---

## File Index

| File | HTTP Method | Endpoint |
|------|-------------|----------|
| `agents-create-prompt.json` | POST | `/api/agents/prompt` |
| `agents-create-hosted.json` | POST | `/api/agents/hosted` |
| `agents-invoke.json` | POST | `/api/agents/{agentName}/invoke` |
| `agents-health.json` | GET | `/api/agents/{agentName}/health` |
| `connections-list.json` | GET | `/api/connections` |
| `datasets-create-file.json` | PUT | `/api/datasets/{name}/versions/{v}/file` |
| `datasets-create-folder.json` | PUT | `/api/datasets/{name}/versions/{v}/folder` |
| `deployments-list.json` | GET | `/api/deployments` |
| `indexes-create-azure-search.json` | PUT | `/api/indexes/{name}/versions/{v}/azure-search` |
| `indexes-create-managed.json` | PUT | `/api/indexes/{name}/versions/{v}/managed` |
| `evaluators-list.json` | GET | `/api/evaluators` |
| `evaluations-create.json` | PUT | `/api/evaluation-rules/{id}` |
| `memory-stores-create.json` | POST | `/api/memory-stores` |
| `memory-stores-search.json` | POST | `/api/memory-stores/{name}/search` |
| `red-teams-create.json` | POST | `/api/red-teams` |
| `telemetry-app-insights.json` | GET | `/api/telemetry/app-insights` |
| `health-global.json` | GET | `/health` (also `/health/live`, `/health/ready`) |

---

## Health Endpoints

Three health endpoints are available for monitoring and orchestration:

| Endpoint | Purpose |
|----------|---------|
| `GET /health` | Full JSON report with per-check status, duration, and errors |
| `GET /health/live` | Liveness probe – returns `200 Healthy` if the process is alive |
| `GET /health/ready` | Readiness probe – returns `200 Healthy` only when Azure AI Foundry is reachable |

Each agent also has its own health endpoint:

```
GET /api/agents/{agentName}/health
```

Returns `Healthy` when the agent exists and is reachable in the Foundry project; `Unhealthy` otherwise.

---

## Invoke an Agent

`POST /api/agents/{agentName}/invoke` creates a conversation thread, sends your message to the agent via the OpenAI Assistants API, polls until the run completes, and returns all messages.  
Pass the returned `threadId` in subsequent calls to maintain a multi-turn conversation.

See [`agents-invoke.json`](agents-invoke.json) for the full example.

---

## Evaluation Rules

Evaluation rules define when agent responses are automatically evaluated.  
`eventType` can be `"ResponseCompleted"` (triggered on every agent response) or `"Manual"` (triggered on demand).

Use `GET /api/evaluators` first to discover the available evaluator names to reference.

See [`evaluations-create.json`](evaluations-create.json) and [`evaluators-list.json`](evaluators-list.json) for examples.

---

## Memory Stores

Memory stores provide persistent, searchable storage for agent conversation context, user profiles, and domain knowledge.  
Create a store with `chatModelDeployment` (for reasoning) and `embeddingModelDeployment` (for indexing), then search it by scope.

See [`memory-stores-create.json`](memory-stores-create.json) and [`memory-stores-search.json`](memory-stores-search.json).

---

## Red Teams

Red teaming simulates adversarial attacks against a model deployment to surface safety vulnerabilities.  
Set `simulationOnly: true` to preview generated attack prompts without making live model calls.

See [`red-teams-create.json`](red-teams-create.json) for both live and simulation-only payload examples.
