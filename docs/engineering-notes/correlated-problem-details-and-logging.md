# Correlated Problem Details and Logging Contract

This note specifies the implementation contract for ADR 0022. It is normative
for API errors and HTTP completion logs.

## Identifiers

- `X-Correlation-ID` is the operator-facing identifier. Web creates or
  propagates a validated value and API returns the same value.
- `traceId` is the 32-character lowercase W3C trace identifier from the active
  `Activity`.
- Every Problem Details response from status 400 through 599 contains both
  identifiers. Neither identifier grants access or carries identity data.

## Problem Details Shape

Responses use `application/problem+json` and contain:

```json
{
  "type": "urn:servicepilot:problem:Customer.EmailAlreadyExists",
  "title": "Conflict",
  "status": 409,
  "code": "Customer.EmailAlreadyExists",
  "detail": "The requested operation conflicts with existing data.",
  "traceId": "07ec88e4dc1bbd459d5f734556d94955",
  "correlationId": "web-request_1234"
}
```

`code` is the stable application contract. Web translates it for users and
uses `title` only as a compatibility fallback. `instance` is omitted because a
raw request path can contain resource identifiers.

Known application errors are exposed only through `ApiErrorCatalog`. A code
missing from that allow-list fails closed as HTTP 500 with
`System.UnmappedError`; its original message is never returned. An architecture
test requires every declared application error to have a catalog entry.

Framework codes are:

| Condition | Code | Status |
| --- | --- | ---: |
| Invalid model or JSON | `Request.ValidationFailed` | 400 |
| Missing or invalid authentication | `Authentication.Required` | 401 |
| Insufficient authorization | `Authorization.Forbidden` | 403 |
| Unknown HTTP endpoint | `Http.NotFound` | 404 |
| Rate limit rejection | `Http.RateLimitExceeded` | 429 |
| Unexpected exception | `System.Unexpected` | 500 |
| Missing application mapping | `System.UnmappedError` | 500 |

Validation responses may contain field names and generic validation messages.
They must not repeat rejected values. A 429 response includes `Retry-After`
when the limiter provides its retry duration.

## Exception Boundary

Unexpected exceptions are handled once by `GlobalExceptionHandler`. The
response contains no exception type, message, stack trace, inner exception,
SQL, file path or request value. Staging and production logs contain the
sanitized exception type but never pass the exception object to `ILogger`.
Framework exception logging is suppressed so it cannot duplicate the event
with an unsafe exception payload.

Development may use local diagnostic tooling, but detailed exceptions are not
exported through the production telemetry path.

## HTTP Completion Log

Every non-health API request produces one `ApiRequestCompleted` event. An
unexpected exception additionally produces one `ApiUnhandledException` event.
Expected client and domain errors do not produce duplicate error logs.

Allowed HTTP completion fields are:

- `RequestMethod`
- `Route` as an ASP.NET route template
- `StatusCode`
- `DurationMs`
- `ProblemCode`
- `TraceId`
- `SpanId`
- `CorrelationId`

The OTLP resource supplies service name, service version and deployment
environment. Raw path, query string, bodies, authorization headers, cookies,
user or tenant IDs, email, phone, exception message and stack trace are not
allowed.

Log levels are:

- 400, 401, 403, 404 and 409: Information
- 429: Warning
- 500 and above: Error

Health-check requests are excluded. All other requests are logged during the
initial low-traffic VPS phase. Sampling may be introduced later, but error and
rate-limit signals must remain available.

## Web Presentation

Web translates the stable `code`. General form and action errors show the
validated correlation ID as a support code with a copy action. Field-level
validation messages do not repeat the support code. Redirected action errors
carry both the translated message and support code in separate query
parameters.
