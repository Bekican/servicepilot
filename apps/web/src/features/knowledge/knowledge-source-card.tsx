import Link from "next/link";
import { BookOpenCheck, ExternalLink } from "lucide-react";

import type { KnowledgeCitation } from "@/lib/api/types";

export function KnowledgeSourceCard({
  citation,
}: {
  citation: KnowledgeCitation;
}) {
  return (
    <Link
      className="hover:border-foreground/30 hover:bg-muted/50 group rounded-xl border bg-white p-3 transition-colors"
      href={`/knowledge/documents/${citation.documentId}/content#page=${citation.pageNumber}`}
      rel="noopener noreferrer"
      target="_blank"
    >
      <span className="flex items-start gap-2">
        <BookOpenCheck className="text-muted-foreground mt-0.5 size-4 shrink-0" />
        <span className="min-w-0 flex-1">
          <span className="block text-xs font-semibold">
            {citation.sourceId} · Sayfa {citation.pageNumber}
          </span>
          <span className="text-muted-foreground mt-0.5 block truncate text-xs">
            {citation.originalFileName}
          </span>
        </span>
        <ExternalLink className="text-muted-foreground size-3.5 transition-transform group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
      </span>
    </Link>
  );
}
