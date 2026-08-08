import { describe, expect, it } from "vitest";

import { removeUntrustedTracingHeaders } from "./trace-headers";

describe("removeUntrustedTracingHeaders", () => {
  it("removes browser-controlled trace context and preserves application headers", () => {
    const headers = new Headers({
      baggage: "tenant.id=secret",
      traceparent: "00-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-bbbbbbbbbbbbbbbb-01",
      tracestate: "vendor=secret",
      "x-correlation-id": "support-code",
    });

    removeUntrustedTracingHeaders(headers);

    expect(headers.has("baggage")).toBe(false);
    expect(headers.has("traceparent")).toBe(false);
    expect(headers.has("tracestate")).toBe(false);
    expect(headers.get("x-correlation-id")).toBe("support-code");
  });
});
