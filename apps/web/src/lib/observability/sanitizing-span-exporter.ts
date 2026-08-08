import type { Attributes } from "@opentelemetry/api";
import { ExportResultCode, type ExportResult } from "@opentelemetry/core";
import { resourceFromAttributes } from "@opentelemetry/resources";
import type { ReadableSpan, SpanExporter } from "@opentelemetry/sdk-trace-base";

const allowedNextSpanNames: ReadonlyMap<string, string> = new Map([
  ["BaseServer.handleRequest", "web.request"],
  ["AppRender.getBodyResult", "next.render"],
  ["AppRouteRouteHandlers.runHandler", "next.route_handler"],
  ["Render.getServerSideProps", "next.server_side_props"],
  ["Render.getStaticProps", "next.static_props"],
  ["Render.renderDocument", "next.render_document"],
  ["ResolveMetadata.generateMetadata", "next.generate_metadata"],
  ["NextNodeServer.findPageComponents", "next.resolve_page"],
  ["NextNodeServer.getLayoutOrPageModule", "next.resolve_module"],
  ["NextNodeServer.startResponse", "next.start_response"],
]);
const nextSpanTypeAttribute = "next.span_type";

const apiTargetAttribute = "servicepilot.trace_target";

const apiTargetValue = "servicepilot.api";

const apiRequestSpanName = "servicepilot.api.request";

const allowedHttpMethods = new Set([
  "GET",
  "POST",
  "PUT",
  "PATCH",
  "DELETE",
  "HEAD",
  "OPTIONS",
]);

type SelectedSpan = {
  source: ReadableSpan;
  safeName: string;
};

export class SanitizingSpanExporter implements SpanExporter {
  constructor(private readonly innerExporter: SpanExporter) {}

  export(
    spans: ReadableSpan[],
    resultCallback: (result: ExportResult) => void,
  ) {
    const safeSpans = sanitizeSpans(spans);

    if (safeSpans.length === 0) {
      resultCallback({
        code: ExportResultCode.SUCCESS,
      });

      return;
    }

    this.innerExporter.export(safeSpans, resultCallback);
  }

  shutdown() {
    return this.innerExporter.shutdown();
  }

  forceFlush() {
    return this.innerExporter.forceFlush?.() ?? Promise.resolve();
  }
}

function sanitizeSpans(spans: ReadableSpan[]): ReadableSpan[] {
  const selectedSpans = selectAllowedSpans(spans);

  return selectedSpans.map(createSafeSpan);
}

function createSafeSpan(selectedSpan: SelectedSpan): ReadableSpan {
  const { source, safeName } = selectedSpan;
  const attributes = createSafeSpanAttributes(source);

  return {
    name: createSafeSpanName(safeName, attributes),
    kind: source.kind,

    spanContext: () => source.spanContext(),
    parentSpanContext: source.parentSpanContext,

    startTime: source.startTime,
    endTime: source.endTime,
    duration: source.duration,
    ended: source.ended,

    status: {
      code: source.status.code,
    },

    attributes,
    events: [],
    links: [],

    resource: resourceFromAttributes({
      "service.name": "ServicePilot.Web",
      "service.version": safeResourceValue(
        source.resource.attributes["service.version"],
      ),
      "deployment.environment.name": safeResourceValue(
        source.resource.attributes["deployment.environment.name"],
      ),
    }),

    instrumentationScope: {
      name: "servicepilot.web.sanitized",
    },

    droppedAttributesCount: 0,
    droppedEventsCount: 0,
    droppedLinksCount: 0,
  };
}

function createSafeSpanAttributes(span: ReadableSpan): Attributes {
  const attributes: Attributes = {};

  const method =
    safeHttpMethod(span.attributes["http.request.method"]) ??
    safeHttpMethod(span.attributes["http.method"]);

  if (method !== undefined) {
    attributes["http.request.method"] = method;
  }

  const route = span.attributes["next.route"];

  if (
    typeof route === "string" &&
    route.startsWith("/") &&
    route.length <= 160 &&
    !/[?#\r\n]/.test(route)
  ) {
    attributes["http.route"] = route;
  }

  const statusCode =
    span.attributes["http.response.status_code"] ??
    span.attributes["http.status_code"];

  if (
    typeof statusCode === "number" &&
    Number.isInteger(statusCode) &&
    statusCode >= 100 &&
    statusCode <= 599
  ) {
    attributes["http.response.status_code"] = statusCode;
  }

  const exceptionType = span.attributes["exception.type"];

  if (
    typeof exceptionType === "string" &&
    /^[A-Za-z_$][A-Za-z0-9_.$+-]{0,159}$/.test(exceptionType)
  ) {
    attributes["exception.type"] = exceptionType;
  }

  return attributes;
}

function safeHttpMethod(value: unknown): string | undefined {
  if (typeof value !== "string") {
    return undefined;
  }

  const normalizedValue = value.toUpperCase();

  return allowedHttpMethods.has(normalizedValue) ? normalizedValue : undefined;
}

function createSafeSpanName(
  defaultName: string,
  attributes: Attributes,
): string {
  if (defaultName !== "web.request") {
    return defaultName;
  }

  const method = attributes["http.request.method"];
  const route = attributes["http.route"];

  return typeof method === "string" && typeof route === "string"
    ? `${method} ${route}`
    : defaultName;
}

function safeResourceValue(value: unknown): string {
  if (
    typeof value === "string" &&
    /^[A-Za-z0-9][A-Za-z0-9._+-]{0,99}$/.test(value)
  ) {
    return value;
  }

  return "unknown";
}

function selectAllowedSpans(spans: ReadableSpan[]): SelectedSpan[] {
  const selectedSpans: SelectedSpan[] = [];

  for (const span of spans) {
    const safeName = resolveSafeSpanName(span);

    if (safeName === undefined) {
      continue;
    }

    selectedSpans.push({
      source: span,
      safeName,
    });
  }

  return selectedSpans;
}

function resolveSafeSpanName(span: ReadableSpan): string | undefined {
  if (span.attributes[apiTargetAttribute] === apiTargetValue) {
    return apiRequestSpanName;
  }

  const nextSpanType = span.attributes[nextSpanTypeAttribute];

  if (typeof nextSpanType !== "string") {
    return undefined;
  }

  return allowedNextSpanNames.get(nextSpanType);
}
