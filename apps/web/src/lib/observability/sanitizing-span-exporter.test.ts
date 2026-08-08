import { SpanKind, SpanStatusCode, type Attributes } from "@opentelemetry/api";
import { ExportResultCode, type ExportResult } from "@opentelemetry/core";
import { resourceFromAttributes } from "@opentelemetry/resources";
import type { ReadableSpan, SpanExporter } from "@opentelemetry/sdk-trace-base";
import { describe, expect, it } from "vitest";

import { SanitizingSpanExporter } from "./sanitizing-span-exporter";

class CapturingExporter implements SpanExporter {
  spans: ReadableSpan[] = [];

  export(
    spans: ReadableSpan[],
    resultCallback: (result: ExportResult) => void,
  ) {
    this.spans.push(...spans);
    resultCallback({ code: ExportResultCode.SUCCESS });
  }

  shutdown() {
    return Promise.resolve();
  }
}

function span(attributes: Attributes): ReadableSpan {
  return {
    name: "raw-user-controlled-name /customers/42?token=secret",
    kind: SpanKind.SERVER,
    spanContext: () => ({
      traceId: "0123456789abcdef0123456789abcdef",
      spanId: "0123456789abcdef",
      traceFlags: 1,
    }),
    parentSpanContext: undefined,
    startTime: [1, 0],
    endTime: [2, 0],
    duration: [1, 0],
    ended: true,
    status: {
      code: SpanStatusCode.ERROR,
      message: "database password was secret",
    },
    attributes,
    events: [
      {
        name: "exception",
        time: [1, 0],
        attributes: { "exception.message": "secret" },
        droppedAttributesCount: 0,
      },
    ],
    links: [],
    resource: resourceFromAttributes({
      "service.name": "attacker-controlled",
      "service.version": "sha-123",
      "deployment.environment.name": "staging",
      "host.name": "private-host",
    }),
    instrumentationScope: { name: "raw-instrumentation" },
    droppedAttributesCount: 3,
    droppedEventsCount: 4,
    droppedLinksCount: 5,
  } as ReadableSpan;
}

function exportSpans(exporter: SanitizingSpanExporter, spans: ReadableSpan[]) {
  return new Promise<ExportResult>((resolve) =>
    exporter.export(spans, resolve),
  );
}

describe("SanitizingSpanExporter", () => {
  it("creates a new allowlisted request span without raw values", async () => {
    const inner = new CapturingExporter();
    const exporter = new SanitizingSpanExporter(inner);
    const raw = span({
      "next.span_type": "BaseServer.handleRequest",
      "http.method": "get",
      "next.route": "/customers/[id]",
      "http.status_code": 200,
      "http.target": "/customers/42?token=secret",
      authorization: "Bearer secret",
      "exception.type": "TypeError",
    });

    const result = await exportSpans(exporter, [raw]);

    expect(result.code).toBe(ExportResultCode.SUCCESS);
    expect(inner.spans).toHaveLength(1);
    const safe = inner.spans[0];
    expect(safe).not.toBe(raw);
    expect(safe.name).toBe("GET /customers/[id]");
    expect(safe.attributes).toEqual({
      "exception.type": "TypeError",
      "http.request.method": "GET",
      "http.response.status_code": 200,
      "http.route": "/customers/[id]",
    });
    expect(safe.status).toEqual({ code: SpanStatusCode.ERROR });
    expect(safe.events).toEqual([]);
    expect(safe.links).toEqual([]);
    expect(safe.resource.attributes).toEqual({
      "deployment.environment.name": "staging",
      "service.name": "ServicePilot.Web",
      "service.version": "sha-123",
    });
    expect(JSON.stringify(safe)).not.toContain("secret");
  });

  it("drops unknown spans before they reach the real exporter", async () => {
    const inner = new CapturingExporter();
    const exporter = new SanitizingSpanExporter(inner);

    const result = await exportSpans(exporter, [
      span({ "next.span_type": "unknown.user.span" }),
    ]);

    expect(result.code).toBe(ExportResultCode.SUCCESS);
    expect(inner.spans).toEqual([]);
  });

  it("exports marked API fetches with a stable name and no marker", async () => {
    const inner = new CapturingExporter();
    const exporter = new SanitizingSpanExporter(inner);

    await exportSpans(exporter, [
      span({
        "servicepilot.trace_target": "servicepilot.api",
        "http.request.method": "POST",
        "url.full": "http://api:8080/api/auth/login?password=secret",
      }),
    ]);

    expect(inner.spans[0].name).toBe("servicepilot.api.request");
    expect(inner.spans[0].attributes).toEqual({
      "http.request.method": "POST",
    });
  });
});
