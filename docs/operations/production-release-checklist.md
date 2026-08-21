# ServicePilot Production Release Checklist

This checklist is the go/no-go contract for the ServicePilot application and
its local knowledge assistant. A successful build is necessary but is not, by
itself, permission to admit customer data.

## Release candidate evidence

The 2026-08-21 release candidate passed:

- .NET build with zero warnings and zero errors
- 92 unit, 16 architecture and 63 PostgreSQL integration tests
- EF Core pending-model-change check
- web formatting, lint, typecheck, 31 component/unit tests and production build
- four desktop/mobile Chromium acceptance journeys
- npm and NuGet vulnerable-package audits with no known findings
- the local Qwen evaluation gate: 100% Recall@3, 100% grounded answers and
  100% correct abstention on the checked dataset
- staging/production environment validator tests

Re-run the source-controlled gate against the exact commit being promoted:

```powershell
.\scripts\verify-mvp.ps1
dotnet list ServicePilot.slnx package --vulnerable --include-transitive
cd apps/web
npm audit --audit-level=high
```

With local Ollama running and both models installed, run:

```powershell
ollama pull qwen3:4b
ollama pull qwen3-embedding:0.6b
dotnet run --project experiments/ServicePilot.AiLab -- eval
```

Do not change the chat model, embedding model or vector dimension without a
new evaluation and a full reindex plan.

## Infrastructure prerequisites

- Ubuntu LTS host: at least 4 vCPU, 12 GiB RAM and 60 GiB disk for the
  CPU-only baseline. Use a compatible GPU and the matching immutable Ollama
  image when response-time measurements require it.
- Real DNS, working TLS issuance, restricted SSH and only ports 80/443 public.
- Unique production database password, JWT signing key, SMTP credentials,
  Restic password and object-storage credentials in a mode-600 environment
  file. Never reuse staging values.
- Every application and infrastructure image pinned by registry digest,
  including PostgreSQL, Ollama, Caddy and the storage initializer.
- `OLLAMA_BASE_URL=http://ollama:11434`. The service is private and is not
  published on the host.
- `DEPLOYMENT_TIER=production`, external TLS SMTP, hosted observability and
  encrypted off-site backup. The validator intentionally rejects production
  when any of these controls is disabled.
- Sender domain SPF, DKIM and DMARC configured and a real invitation/password
  reset delivery verified.

## Deployment order

1. Run the full source and AI gates above.
2. Publish immutable application images for the exact commit and build the
   private release manifest with resolved digests.
3. Validate `/etc/servicepilot/production.env` without printing its secrets.
4. Deploy the candidate. The deployment script starts PostgreSQL and private
   Ollama, applies forward-only migrations, prepares both Qwen models and the
   document volume, then replaces application containers.
5. Confirm all three public operational checks return success:

```sh
curl --fail https://servicepilot.example.com/ops/api/ready
curl --fail https://servicepilot.example.com/ops/api/knowledge
curl --fail https://servicepilot.example.com/ops/web/ready
```

6. Upload one text PDF and one scanned Turkish PDF. Wait for `Ready`, ask an
   answerable and an unanswerable question, and open every returned citation.
7. Verify cross-role visibility using Owner, Dispatcher and Technician test
   users. A customer service report must never be visible through `Shared`.
8. Verify rate-limit responses are understandable and that the rest of the
   product remains usable when Ollama is deliberately stopped.
9. Send a real invitation and password-reset email, then verify token expiry
   and single use.

## Data protection and recovery gate

The PostgreSQL dump and private knowledge-file volume form one recovery point.
Before the first customer is admitted:

```sh
/bin/sh scripts/initialize-restic-staging.sh
/bin/sh scripts/backup-staging.sh
/bin/sh scripts/restore-drill-staging.sh
```

The drill must restore into isolation, match non-PII table counts, validate the
knowledge-file checksum manifest and prove every active storage key exists.
Record the snapshot ID, duration, operator and result. Enable the nightly timer
and a dead-man-switch alert. Rehearse application rollback separately; never
run an automatic down migration.

## Landing application

The separate `servicepilot-landingpage` project is deployable independently.
Set both variables before its production build:

```dotenv
NEXT_PUBLIC_SITE_URL=https://www.example.com
NEXT_PUBLIC_APP_URL=https://servicepilot.example.com
```

Build its standalone container, verify `/api/health`, canonical metadata,
robots/sitemap and the CTA destination. The landing application contains no
customer data and must not proxy the private API.

## Go/no-go rule

Go only when the exact release passed staging, the AI health and evaluation
gates are green, a backup and restore drill succeeded, alert delivery was
observed, transactional email works and the rollback rehearsal is recorded.
Any missing item is a no-go; it is an operational prerequisite, not an
application warning to waive.
