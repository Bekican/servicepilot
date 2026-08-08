import type {
  Context,
  TextMapGetter,
  TextMapPropagator,
  TextMapSetter,
} from "@opentelemetry/api";

import { W3CTraceContextPropagator } from "@opentelemetry/core";

export class OutboundOnlyTraceContextPropagator implements TextMapPropagator {
  private readonly traceContext = new W3CTraceContextPropagator();

  inject(context: Context, carrier: unknown, setter: TextMapSetter) {
    this.traceContext.inject(context, carrier, setter);
  }
  extract(context: Context, _carrier: unknown, _getter: TextMapGetter) {
    return context;
  }

  fields() {
    return this.traceContext.fields();
  }
}
