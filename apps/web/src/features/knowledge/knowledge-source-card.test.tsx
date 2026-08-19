import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { KnowledgeSourceCard } from "@/features/knowledge/knowledge-source-card";

describe("KnowledgeSourceCard", () => {
  it("opens the authorized PDF proxy on the cited page", () => {
    render(
      <KnowledgeSourceCard
        citation={{
          sourceId: "S2",
          documentId: "11111111-1111-1111-1111-111111111111",
          originalFileName: "kombi-bakim.pdf",
          pageNumber: 7,
          contentUrl: "/api/knowledge/documents/ignored/content#page=7",
        }}
      />,
    );

    const link = screen.getByRole("link", { name: /S2 · Sayfa 7/i });
    expect(link.getAttribute("href")).toBe(
      "/knowledge/documents/11111111-1111-1111-1111-111111111111/content#page=7",
    );
    expect(link.getAttribute("target")).toBe("_blank");
    expect(screen.getByText("kombi-bakim.pdf")).toBeTruthy();
  });
});
