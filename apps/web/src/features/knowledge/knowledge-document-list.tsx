"use client";

import { useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ExternalLink, FileText, RefreshCw } from "lucide-react";

import { PendingButton } from "@/components/shared/pending-button";
import { StatusBadge } from "@/components/shared/status-badge";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { retryKnowledgeDocumentAction } from "@/features/knowledge/actions";
import type { KnowledgeDocument } from "@/lib/api/types";
import { formatDate } from "@/lib/date";

const typeLabels: Record<string, string> = {
  TechnicalProcedure: "Teknik prosedür",
  Manual: "Kılavuz",
  CustomerServiceReport: "Servis raporu",
  Warranty: "Garanti",
  Other: "Diğer",
};

const scopeLabels: Record<string, string> = {
  Shared: "Tüm ekip",
  Operations: "Operasyon",
  Management: "Yönetim",
};

export function KnowledgeDocumentList({
  documents,
  timeZone,
  canManage,
}: {
  documents: KnowledgeDocument[];
  timeZone: string;
  canManage: boolean;
}) {
  const router = useRouter();
  const processing = documents.some((document) =>
    ["Pending", "Processing"].includes(document.status),
  );

  useEffect(() => {
    if (!processing) return;
    const timer = window.setInterval(() => router.refresh(), 5000);
    return () => window.clearInterval(timer);
  }, [processing, router]);

  if (!documents.length) {
    return (
      <div className="rounded-lg border border-dashed px-5 py-10 text-center">
        <FileText className="text-muted-foreground mx-auto size-8" />
        <p className="mt-3 font-medium">Henüz belge yok</p>
        <p className="text-muted-foreground mt-1 text-xs">
          İlk PDF&apos;yi yüklediğinizde burada görünecek.
        </p>
      </div>
    );
  }

  return (
    <div className="divide-y">
      {documents.map((document) => (
        <article className="py-4 first:pt-0 last:pb-0" key={document.id}>
          <div className="flex items-start gap-3">
            <span className="bg-muted mt-0.5 grid size-9 shrink-0 place-items-center rounded-lg">
              <FileText className="text-muted-foreground size-4" />
            </span>
            <div className="min-w-0 flex-1">
              <p
                className="truncate font-medium"
                title={document.originalFileName}
              >
                {document.originalFileName}
              </p>
              <div className="mt-1.5 flex flex-wrap items-center gap-1.5">
                <StatusBadge status={document.status} />
                <Badge variant="outline">
                  {typeLabels[document.documentType] ?? document.documentType}
                </Badge>
                <Badge variant="secondary">
                  {scopeLabels[document.accessScope] ?? document.accessScope}
                </Badge>
              </div>
              <p className="text-muted-foreground mt-2 text-xs">
                {formatBytes(Number(document.sizeBytes))} ·{" "}
                {formatDate(document.createdAtUtc, timeZone)}
              </p>
              {document.status === "Processing" ||
              document.status === "Pending" ? (
                <p className="mt-2 flex items-center gap-1.5 text-xs text-orange-700">
                  <RefreshCw className="size-3 animate-spin" />
                  Soru-cevap için hazırlanıyor
                </p>
              ) : null}
              {document.status === "Failed" ? (
                <p className="text-destructive mt-2 text-xs">
                  {document.lastErrorMessage ?? "Belge işlenemedi."}
                </p>
              ) : null}
              <div className="mt-3 flex gap-2">
                <Button asChild size="sm" variant="outline">
                  <Link
                    href={`/knowledge/documents/${document.id}/content`}
                    target="_blank"
                  >
                    PDF&apos;yi aç
                    <ExternalLink />
                  </Link>
                </Button>
                {canManage && document.status === "Failed" ? (
                  <form
                    action={retryKnowledgeDocumentAction.bind(
                      null,
                      document.id,
                    )}
                  >
                    <PendingButton
                      pendingLabel="Sıraya alınıyor…"
                      size="sm"
                      type="submit"
                      variant="ghost"
                    >
                      <RefreshCw />
                      Tekrar işle
                    </PendingButton>
                  </form>
                ) : null}
              </div>
            </div>
          </div>
        </article>
      ))}
    </div>
  );
}

function formatBytes(bytes: number) {
  if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`;
  return `${(bytes / (1024 * 1024)).toLocaleString("tr-TR", {
    maximumFractionDigits: 1,
  })} MB`;
}
