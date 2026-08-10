import { describe, expect, it } from "vitest";

import { organizationSlugFromName } from "./organization-slug";

describe("organizationSlugFromName", () => {
  it("converts a Turkish organization name into a safe slug", () => {
    expect(organizationSlugFromName("Şahin Çağrı Teknik Servis")).toBe(
      "sahin-cagri-teknik-servis",
    );
  });

  it("collapses punctuation and whitespace into optional hyphens", () => {
    expect(organizationSlugFromName("  Atlas & Ortaklar!!! ")).toBe(
      "atlas-ortaklar",
    );
  });

  it("allows a single segment without requiring a hyphen", () => {
    expect(organizationSlugFromName("ServicePilot")).toBe("servicepilot");
  });
});
