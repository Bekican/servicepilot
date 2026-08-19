# ServicePilot AI Lab

This console project verifies the local model contracts before they are added
to production ServicePilot projects. It never uses a cloud fallback.

## Prerequisites

Install and start Ollama, then make sure both local models exist:

```powershell
ollama pull qwen3:4b
ollama pull qwen3-embedding:0.6b
ollama list
```

## Run

Run all experiments:

```powershell
dotnet run --project experiments/ServicePilot.AiLab -- all
```

Run one experiment:

```powershell
dotnet run --project experiments/ServicePilot.AiLab -- structured
dotnet run --project experiments/ServicePilot.AiLab -- embed
```

The structured experiment verifies both a grounded answer with an inline
citation and abstention when the sources do not contain an answer. The
embedding experiment verifies that the relevant Turkish maintenance passage
ranks above unrelated passages by cosine similarity.

## Optional configuration

The defaults are suitable for local development. Override them only with local
models and an approved local endpoint:

```powershell
$env:OLLAMA_BASE_URL = "http://localhost:11434"
$env:OLLAMA_CHAT_MODEL = "qwen3:4b"
$env:OLLAMA_EMBEDDING_MODEL = "qwen3-embedding:0.6b"
```

Cloud-tagged models and non-local addresses are rejected by the lab.
