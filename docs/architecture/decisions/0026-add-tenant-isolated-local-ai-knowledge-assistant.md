# ADR 0026: Add a Tenant-Isolated Local AI Knowledge Assistant

- Status: Accepted
- Date: 2026-08-19

## Context

ServicePilot organizations need to search customer-specific service reports,
technical service procedures, manuals, warranty documents and similar PDF
documents. Users should be able to ask questions in natural language, receive
answers grounded in those documents and open the cited PDF page directly.

The first version must keep document content, questions and model output on the
local machine. It must preserve the existing authenticated tenant boundary and
must not make the rest of ServicePilot unavailable when the AI runtime is down.
The design should still permit a future move to a cloud model through an
explicit configuration and deployment decision.

## Product Scope

The first version will provide:

- Text-based PDF upload and lifecycle management
- Background text extraction, chunking and indexing
- Tenant- and role-scoped semantic retrieval
- Grounded question answering in the language used by the user
- Server-validated citations linked to an authorized PDF viewer
- Private per-user conversation history
- Visible processing failures and retry

The first version will not provide OCR, image understanding, internet search,
live customer or appointment queries, function calling, write actions, model
fine-tuning or automatic cloud fallback.

## Decision

ServicePilot will implement a retrieval-augmented generation pipeline using:

- Ollama as the local model runtime
- `qwen3:4b` for schema-constrained answer generation
- `qwen3-embedding:0.6b` for text embeddings
- PostgreSQL 17 with pgvector for vector storage and similarity search
- Local filesystem storage behind an application-owned storage abstraction
- The existing Worker process for extraction and indexing

Generation, embedding, file storage, extraction and retrieval will have
provider-neutral Application interfaces. Ollama, pgvector, PDF extraction and
local filesystem details will remain in Infrastructure or host projects.
There will be no automatic fallback to a cloud provider.

## Data Flow

An upload is first validated and stored under a generated storage key. A
tenant-owned `KnowledgeDocument` is committed with `Pending` status. The Worker
claims pending work with a bounded lease, extracts text page by page, creates
page-bound chunks, requests embeddings in batches and stores the chunks and
vectors. The document becomes searchable only after the complete index is
committed and its status is `Ready`.

A question is embedded locally. Retrieval filters by organization, document
status and the current user's authorized document scopes before ranking by
cosine distance. Only the selected evidence is sent to the local generation
model. The model returns a structured answer containing source identifiers.
The server verifies every identifier against the supplied evidence before the
answer is persisted or returned.

## Tenant Isolation

HTTP operations obtain the organization only from the authenticated tenant
context. Request contracts will not accept `OrganizationId`. Background work
carries a trusted persisted organization identifier.

Every document, chunk, conversation and message is tenant-owned. Retrieval
must apply the tenant and access filters in the database query. Tenant
isolation will never depend on a prompt instruction. A document or citation
outside the caller's boundary returns not found and its existence is not
revealed.

## Authorization

Two capabilities will be introduced:

- `ManageKnowledgeDocuments`: Owner and Admin
- `UseKnowledgeAssistant`: every active Owner, Admin, Dispatcher and Technician

Documents have one of three access scopes:

- `Shared`: every active role
- `Operations`: Owner, Admin and Dispatcher
- `Management`: Owner and Admin

Technical procedures and manuals default to `Shared`. Customer service
reports, warranty documents and unknown document types default to
`Operations`. Customer service reports cannot be made `Shared` in the first
version. Per-user and appointment-derived access are deferred until a concrete
workflow justifies their complexity.

The same authorization decision is applied when listing documents, retrieving
chunks and opening the source PDF. Conversations are visible only to their
creator; Owner and Admin roles do not implicitly gain access to other users'
conversation content.

## Document Storage and Lifecycle

PDF bytes will not be stored in publicly served directories or in telemetry.
The local implementation uses generated paths shaped as:

```text
knowledge/{organizationId}/{documentId}/source.pdf
```

The original filename is metadata only and never becomes a filesystem path.
The API streams authorized PDFs inline and supports browser range requests.
Citation links open an application route that rechecks authorization before
serving the PDF.

Documents move through `Pending`, `Processing`, `Ready`, `Failed`, `Deleting`
and `Deleted` states. Deletion immediately removes a document from retrieval;
physical cleanup is retryable. A SHA-256 checksum prevents duplicate active
content inside one organization. Worker operations are idempotent and use
bounded attempts with visible failure codes.

## PDF Acceptance Rules

The first version accepts text-based PDFs up to 20 MB and 300 pages. It checks
the content type, PDF signature, safe filename length and extraction bounds.
Encrypted, malformed and textless PDFs are rejected with actionable errors.
Textless PDFs explicitly report that OCR is not supported instead of silently
creating an empty index.

## Chunking and Retrieval

Extraction preserves page boundaries so every chunk has an unambiguous page
citation. The initial chunker targets 1,200 characters, caps chunks at 1,800
characters and carries approximately 200 characters of overlap without
crossing a page boundary. These values are baselines and may change only after
evaluation.

The query embedding includes an English retrieval instruction describing the
Turkish technical-service domain. Document chunks contain only document text.
The initial exact cosine search retrieves twelve candidates, keeps at most
three chunks from one document and supplies at most eight chunks to the model.
An approximate index, hybrid keyword search and reranking are deferred until
the evaluation set demonstrates their need.

The embedding model name and vector dimension are recorded with indexed data.
Changing either requires reindexing; vectors from different models are never
compared.

## Answer Contract

Retrieved evidence receives server-generated identifiers such as `S1` and
`S2`. The model must return a JSON-schema-constrained result containing the
answer, cited identifiers and an insufficient-evidence flag. It is instructed
to treat all document content as untrusted data, ignore instructions found
inside documents and answer only from supplied evidence.

The server rejects unknown or inaccessible citations. If evidence is absent or
insufficient, the application ignores model answer text and returns a fixed,
localized message saying that the uploaded documents do not contain the
answer. This refusal wording does not depend on model behavior. Deleted
sources remain represented in old conversations as unavailable rather than
bypassing current authorization.

## Local-Only Boundary

AI configuration selects Ollama explicitly and disables cloud fallback. Local
configuration rejects cloud-tagged models. Document contents, questions,
answers, source passages and embeddings are prohibited from logs and
telemetry. The Ollama base address must resolve to an explicitly configured
local or private endpoint.

Ollama availability is reported through a separate AI status signal. It does
not fail the main API readiness check or prevent non-AI ServicePilot workflows.

## Observability

AI operations may record model name, operation type, duration, input and output
token counts, retrieved chunk count, result category and sanitized provider
request metadata. Metrics and traces must not include tenant identifiers,
filenames, document text, questions, answers, embeddings or customer data.

## Verification

Automated tests will cover domain transitions, duplicate uploads, role scopes,
cross-tenant reads, authorized PDF streaming, Worker retry and idempotency,
provider failures, citation validation, insufficient evidence and telemetry
sanitization.

Normal unit and integration tests use deterministic fake generation and
embedding clients. PostgreSQL integration tests use a pgvector-enabled image.
Real Ollama checks live in an explicit local evaluation tool and are not a
prerequisite for ordinary CI.

The evaluation dataset includes answerable, unanswerable, multilingual,
identifier-heavy, misleading, prompt-injection and cross-tenant cases. The
initial release requires zero cross-tenant leakage, zero invented citations,
at least 95 percent citation precision, at least 90 percent retrieval recall
at eight and at least 95 percent correct abstention on unanswerable questions.

## Consequences

### Positive

- Sensitive AI inputs remain local in the first deployment.
- Tenant and role checks occur before model invocation.
- Existing PostgreSQL, Worker, authorization and observability patterns are
  reused.
- Provider-neutral boundaries permit an intentional future cloud migration.
- Retrieval and generation can be evaluated independently.

### Negative

- Local model quality and latency are limited by available hardware.
- pgvector and shared document storage add deployment and backup concerns.
- Text-only PDF extraction does not support scanned documents.
- Exact vector search will require reconsideration as chunk counts grow.
- Local Ollama availability becomes an operational dependency for AI features.

## Reconsideration Triggers

Revisit this decision if OCR becomes a core workflow, local model quality does
not meet the evaluation gates, vector volume makes exact search too slow,
technicians require per-customer document access, a cloud provider is approved
for sensitive content, or AI workloads require independent deployment and
scaling.
