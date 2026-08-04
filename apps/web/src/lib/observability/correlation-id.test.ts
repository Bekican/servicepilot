import { describe, expect, it } from "vitest";

import {
  isValidCorrelationId,
  resolveCorrelationId,
} from "@/lib/observability/correlation-id";

describe("correlation IDs", () => {
  it("preserves a valid incoming value", () => {
    expect(resolveCorrelationId("web-request_1234")).toBe("web-request_1234");
  });

  it.each([
    null,
    "",
    "short",
    "contains spaces",
    "contains/slash",
    "a".repeat(65),
  ])("replaces an absent or unsafe value: %s", (value) => {
    const resolved = resolveCorrelationId(value);

    expect(resolved).toMatch(/^[a-f0-9]{32}$/);
    expect(isValidCorrelationId(resolved)).toBe(true);
  });
});
