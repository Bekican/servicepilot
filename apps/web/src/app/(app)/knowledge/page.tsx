import type { Metadata } from "next";
import { BookOpenCheck, HardDrive, ShieldCheck } from "lucide-react";

import { ActionMessage } from "@/components/shared/action-message";
import { PageHeader } from "@/components/shared/page-header";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { KnowledgeChat } from "@/features/knowledge/knowledge-chat";
import { KnowledgeDocumentList } from "@/features/knowledge/knowledge-document-list";
import { KnowledgeUploadDialog } from "@/features/knowledge/knowledge-upload-dialog";
import { createServerApiClient } from "@/lib/api/server-client";
import type { KnowledgeDocument } from "@/lib/api/types";
import { requireSession } from "@/lib/auth/session";

export const metadata: Metadata = { title: "Bilgi Asistanı" };

export default async function KnowledgePage({
  searchParams,
}: {
  searchParams: Promise<{
    error?: string;
    success?: string;
    supportCode?: string;
  }>;
}) {
  const query = await searchParams;
  const session = await requireSession();
  const client = await createServerApiClient();
  const { data } = await client.GET("/api/knowledge/documents");
  const documents = (data ?? []) as KnowledgeDocument[];
  const readyDocumentCount = documents.filter(
    (document) => document.status === "Ready",
  ).length;
  const canManage = session.capabilities.includes("ManageKnowledgeDocuments");

  return (
    <>
      <PageHeader
        action={canManage ? <KnowledgeUploadDialog /> : undefined}
        description="Şirket belgelerinizden kaynaklı ve doğrulanabilir teknik yanıtlar alın."
        title="Bilgi Asistanı"
      />
      <ActionMessage
        error={query.error}
        success={query.success}
        supportCode={query.supportCode}
      />

      <div className="mb-5 grid gap-3 sm:grid-cols-2">
        <div className="flex items-center gap-3 rounded-xl border bg-white px-4 py-3">
          <span className="grid size-9 place-items-center rounded-lg bg-emerald-50 text-emerald-700">
            <ShieldCheck className="size-4" />
          </span>
          <div>
            <p className="text-sm font-medium">Şirket içinde ve yetkili</p>
            <p className="text-muted-foreground text-xs">
              Yalnızca rolünüzün erişebildiği belgeler aranır.
            </p>
          </div>
        </div>
        <div className="flex items-center gap-3 rounded-xl border bg-white px-4 py-3">
          <span className="grid size-9 place-items-center rounded-lg bg-violet-50 text-violet-700">
            <HardDrive className="size-4" />
          </span>
          <div>
            <p className="text-sm font-medium">Tamamen yerel yapay zekâ</p>
            <p className="text-muted-foreground text-xs">
              Sorularınız ve belgeleriniz makine dışına çıkmaz.
            </p>
          </div>
        </div>
      </div>

      <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,1.7fr)_390px]">
        <Card className="gap-0 overflow-hidden py-0">
          <CardHeader className="border-b py-4">
            <CardTitle className="flex items-center gap-2">
              <BookOpenCheck className="size-5 text-violet-700" />
              Belgelerinize sorun
            </CardTitle>
            <CardDescription>
              Hazır {readyDocumentCount} belge içinde kaynaklı arama
            </CardDescription>
          </CardHeader>
          <KnowledgeChat readyDocumentCount={readyDocumentCount} />
        </Card>

        <Card className="xl:sticky xl:top-24">
          <CardHeader className="border-b">
            <div className="flex items-start justify-between gap-3">
              <div>
                <CardTitle>Bilgi kaynakları</CardTitle>
                <CardDescription className="mt-1">
                  {documents.length} belge · {readyDocumentCount} kullanıma
                  hazır
                </CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent className="max-h-[680px] overflow-y-auto">
            <KnowledgeDocumentList
              canManage={canManage}
              documents={documents}
              timeZone={session.timeZoneId}
            />
          </CardContent>
        </Card>
      </div>
    </>
  );
}
