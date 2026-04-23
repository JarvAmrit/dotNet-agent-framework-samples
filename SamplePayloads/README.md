# Sample Payloads

This folder contains ready-to-use JSON payloads and `curl` examples for every endpoint in the Azure AI Foundry API.

## Structure

```
SamplePayloads/
├── Agents/
│   ├── POST_prompt.json          POST /api/agents/prompt
│   ├── POST_hosted.json          POST /api/agents/hosted
│   ├── POST_invoke.json          POST /api/agents/{agentName}/invoke  (new conversation)
│   └── POST_invoke_continue.json POST /api/agents/{agentName}/invoke  (continue conversation)
├── Connections/
│   └── GET_list.json             GET /api/connections  (query param examples)
├── Deployments/
│   └── GET_list.json             GET /api/deployments  (query param examples)
├── Indexes/
│   ├── PUT_azure-search.json     PUT /api/indexes/{indexName}/versions/{v}/azure-search
│   └── PUT_managed.json          PUT /api/indexes/{indexName}/versions/{v}/managed
├── Datasets/
│   ├── PUT_file.json             PUT /api/datasets/{datasetName}/versions/{v}/file
│   └── PUT_folder.json           PUT /api/datasets/{datasetName}/versions/{v}/folder
└── Health/
    ├── GET_overall.json          Response example: GET /api/health
    └── GET_agent.json            Response example: GET /api/health/agents/{agentName}
```

## Base URL

Replace `https://localhost:5001` with your actual API host when deployed.

## Quick tip

For `PUT` and `POST` payloads, copy the JSON body and pass it as `-d @<file>` in `curl`:

```bash
curl -X POST https://localhost:5001/api/agents/prompt \
  -H "Content-Type: application/json" \
  -d @SamplePayloads/Agents/POST_prompt.json
```
