import { describe, expect, it } from "vitest";

import { problemMessage } from "@/lib/api/problem-details";

describe("problemMessage", () => {
  it("translates known domain errors", () => {
    expect(
      problemMessage({ title: "Appointment.TechnicianOverlap" }),
    ).toContain("başka bir randevusu");
  });

  it("uses API detail for unknown errors", () => {
    expect(problemMessage({ detail: "Özel hata" })).toBe("Özel hata");
  });

  it("falls back when the response has no problem body", () => {
    expect(problemMessage(undefined, "Tekrar deneyin")).toBe("Tekrar deneyin");
  });
});
