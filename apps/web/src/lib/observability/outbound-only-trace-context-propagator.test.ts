import {
  ROOT_CONTEXT,
  TraceFlags,
  trace,
  type TextMapGetter,
  type TextMapSetter,
} from "@opentelemetry/api";
import { describe, expect, it } from "vitest";

import { OutboundOnlyTraceContextPropagator } from "./outbound-only-trace-context-propagator";

const getter: TextMapGetter<Record<string, string>> = {
  get: (carrier, key) => carrier[key],
  keys: (carrier) => Object.keys(carrier),
};

const setter: TextMapSetter<Record<string, string>> = {
  set: (carrier, key, value) => {
    carrier[key] = value;
  },
};

describe("OutboundOnlyTraceContextPropagator", () => {
  it("ignores inbound browser trace context", () => {
    const propagator = new OutboundOnlyTraceContextPropagator();
    const carrier = {
      traceparent: "00-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-bbbbbbbbbbbbbbbb-01",
      tracestate: "vendor=secret",
      baggage: "tenant.id=secret",
    };

    const extracted = propagator.extract(ROOT_CONTEXT, carrier, getter);

    expect(extracted).toBe(ROOT_CONTEXT);
    expect(trace.getSpanContext(extracted)).toBeUndefined();
  });

  it("injects only the server-created W3C trace context", () => {
    const propagator = new OutboundOnlyTraceContextPropagator();
    const context = trace.setSpanContext(ROOT_CONTEXT, {
      traceId: "0123456789abcdef0123456789abcdef",
      spanId: "0123456789abcdef",
      traceFlags: TraceFlags.SAMPLED,
    });
    const carrier: Record<string, string> = {};

    propagator.inject(context, carrier, setter);

    expect(carrier.traceparent).toBe(
      "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01",
    );
    expect(carrier.tracestate).toBeUndefined();
    expect(carrier.baggage).toBeUndefined();
    expect(propagator.fields()).toEqual(["traceparent", "tracestate"]);
  });
});
