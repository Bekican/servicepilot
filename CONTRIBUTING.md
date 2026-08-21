# Contributing to ServicePilot

Thank you for taking the time to improve ServicePilot. The project favors
small, reviewable changes with an explicit reason and a regression test.

## Before opening a change

1. Search existing issues and architecture decisions.
2. Open an issue first for a new capability or a change to a security,
   multi-tenancy, data-retention or RAG trust boundary.
3. Create a focused branch from an up-to-date `main`.
4. Never add real customer data, documents, credentials or generated local
   environment files.

## Engineering expectations

- Preserve the dependency direction documented in the root README.
- Enforce tenant and role isolation before data reaches an LLM context.
- Add a failing regression test before changing behavior where practical.
- Keep API contracts, generated frontend types and user-facing errors aligned.
- Record durable architectural choices as an ADR.
- Prefer explicit failure and recovery states over silent fallback behavior.

## Validate locally

Run the complete repository gate from the root:

```powershell
./scripts/verify-mvp.ps1
```

If a change affects real Ollama behavior, also run:

```powershell
dotnet run --project experiments/ServicePilot.AiLab -- eval
```

## Pull requests

Keep one concern per pull request. Explain the user impact, architectural
impact, tests performed and any security or operational considerations. The PR
must pass quality and security checks before merge.
