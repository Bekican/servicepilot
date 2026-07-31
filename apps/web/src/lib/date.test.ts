import { describe, expect, it } from "vitest";

import { dateKey, formatTime, zonedLocalDateTimeToIso } from "@/lib/date";

describe("organization time helpers", () => {
  it("converts an Istanbul wall clock value to UTC", () => {
    expect(zonedLocalDateTimeToIso("2026-07-31T14:30", "Europe/Istanbul")).toBe(
      "2026-07-31T11:30:00.000Z",
    );
  });

  it("formats and groups instants in the organization time zone", () => {
    const instant = "2026-07-31T21:30:00.000Z";

    expect(dateKey(instant, "Europe/Istanbul")).toBe("2026-08-01");
    expect(formatTime(instant, "Europe/Istanbul")).toMatch(/00[.:]30/);
  });
});
