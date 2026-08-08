const untrustedTracingHeaders = ["traceparent", "tracestate", "baggage"];

export function removeUntrustedTracingHeaders(headers: Headers) {
  for (const header of untrustedTracingHeaders) {
    headers.delete(header);
  }
}
