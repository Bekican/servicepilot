import { describe, expect, it } from "vitest";

import {
  problemMessage,
  problemActionState,
  problemPresentation,
  problemSearchParams,
} from "@/lib/api/problem-details";

describe("problemMessage", () => {
  it("translates known domain errors", () => {
    expect(
      problemMessage({ title: "Appointment.TechnicianOverlap" }),
    ).toContain("başka bir randevusu");
  });

  it("prefers the stable problem code over the title", () => {
    expect(
      problemMessage({
        code: "Appointment.TechnicianOverlap",
        title: "Conflict",
      }),
    ).toContain("randevusu");
  });

  it("exposes the correlation ID as a support code", () => {
    expect(
      problemPresentation({
        code: "System.Unexpected",
        correlationId: "web-request_1234",
      }),
    ).toMatchObject({ supportCode: "web-request_1234" });
  });

  it("includes the support code in redirect parameters", () => {
    const params = new URLSearchParams(
      problemSearchParams({
        detail: "Unexpected",
        correlationId: "web-request_1234",
      }),
    );

    expect(params.get("error")).toBe("Unexpected");
    expect(params.get("supportCode")).toBe("web-request_1234");
  });

  it("uses API detail for unknown errors", () => {
    expect(problemMessage({ detail: "Özel hata" })).toBe("Özel hata");
  });

  it("falls back when the response has no problem body", () => {
    expect(problemMessage(undefined, "Tekrar deneyin")).toBe("Tekrar deneyin");
  });

  it("translates field error codes without exposing a general error", () => {
    expect(
      problemActionState({
        code: "Validation.Failed",
        errors: {
          organizationSlug: ["InvalidSlug"],
        },
      }),
    ).toMatchObject({
      error: undefined,
      fieldErrors: {
        organizationSlug: expect.stringContaining("küçük harf"),
      },
    });
  });

  it("falls back to the general error when model binding keys are not form fields", () => {
    expect(
      problemActionState({
        code: "Appointment.InvalidData",
        errors: {
          "$.startAt": ["Invalid"],
          request: ["Invalid"],
        },
      }),
    ).toMatchObject({
      error: expect.stringContaining("Randevu zamanı"),
      fieldErrors: undefined,
    });
  });
});
