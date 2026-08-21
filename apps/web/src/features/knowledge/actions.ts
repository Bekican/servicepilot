"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import {
  problemPresentation,
  problemSearchParams,
} from "@/lib/api/problem-details";
import { createServerApiClient, fetchServerApi } from "@/lib/api/server-client";
import type { ProblemDetails } from "@/lib/api/types";
import { hasCapability, requireSession } from "@/lib/auth/session";

import type { KnowledgeConversationState } from "./types";

const maximumPdfBytes = 20 * 1024 * 1024;

export type KnowledgeUploadState = {
  error?: string;
  supportCode?: string;
};

export async function askKnowledgeAction(
  previous: KnowledgeConversationState,
  formData: FormData,
): Promise<KnowledgeConversationState> {
  const session = await requireSession();
  if (!hasCapability(session, "UseKnowledgeAssistant")) {
    return { ...previous, error: "Bilgi asistanını kullanma yetkiniz yok." };
  }

  const question = String(formData.get("question") ?? "").trim();
  if (!question || question.length > 2000) {
    return {
      ...previous,
      error: "Sorunuz 1 ile 2000 karakter arasında olmalıdır.",
    };
  }

  const client = await createServerApiClient();
  const { data, error } = await client.POST("/api/knowledge/assistant/ask", {
    body: { question },
  });
  const messages = [
    ...previous.messages,
    { role: "user" as const, content: question },
  ].slice(-11);

  if (!data) {
    const presentation = problemPresentation(
      error,
      "Bilgi asistanına şu anda ulaşılamıyor. Lütfen tekrar deneyin.",
    );
    return {
      messages,
      error: presentation.message,
      supportCode: presentation.supportCode,
    };
  }

  return {
    messages: [
      ...messages,
      {
        role: "assistant" as const,
        content: data.answer,
        insufficientEvidence: data.insufficientEvidence,
        citations: data.citations,
      },
    ].slice(-12),
  };
}

export async function uploadKnowledgeDocumentAction(
  _previous: KnowledgeUploadState,
  formData: FormData,
): Promise<KnowledgeUploadState> {
  const session = await requireSession();
  if (!hasCapability(session, "ManageKnowledgeDocuments")) {
    return { error: "Belge yükleme yetkiniz yok." };
  }

  const file = formData.get("file");
  if (!(file instanceof File) || file.size === 0) {
    return { error: "Yüklemek için bir PDF dosyası seçin." };
  }
  if (
    file.type !== "application/pdf" ||
    !file.name.toLowerCase().endsWith(".pdf")
  ) {
    return { error: "Yalnızca PDF dosyaları yüklenebilir." };
  }
  if (file.size > maximumPdfBytes) {
    return { error: "PDF dosyası en fazla 20 MB olabilir." };
  }

  const upload = new FormData();
  upload.set("file", file);
  upload.set("documentType", String(formData.get("documentType") ?? "Other"));
  upload.set(
    "accessScope",
    String(formData.get("accessScope") ?? "Operations"),
  );
  const response = await fetchServerApi("/api/knowledge/documents", {
    method: "POST",
    body: upload,
  });
  if (!response.ok) {
    const problem = (await response.json().catch(() => undefined)) as
      ProblemDetails | undefined;
    const presentation = problemPresentation(
      problem,
      "Belge yüklenemedi. Lütfen dosyayı kontrol edip tekrar deneyin.",
    );
    return {
      error: presentation.message,
      supportCode: presentation.supportCode,
    };
  }

  revalidatePath("/knowledge");
  redirect(
    `/knowledge?success=${encodeURIComponent("Belge yüklendi ve işleme sırasına alındı.")}`,
  );
}

export async function retryKnowledgeDocumentAction(documentId: string) {
  const session = await requireSession();
  if (!hasCapability(session, "ManageKnowledgeDocuments")) {
    redirect(
      `/knowledge?error=${encodeURIComponent("Belge yönetme yetkiniz yok.")}`,
    );
  }

  const client = await createServerApiClient();
  const { response, error } = await client.POST(
    "/api/knowledge/documents/{id}/retry",
    { params: { path: { id: documentId } } },
  );
  if (!response.ok) {
    redirect(`/knowledge?${problemSearchParams(error)}`);
  }

  revalidatePath("/knowledge");
  redirect(
    `/knowledge?success=${encodeURIComponent("Belge yeniden işleme sırasına alındı.")}`,
  );
}

export async function changeKnowledgeDocumentScopeAction(
  documentId: string,
  formData: FormData,
) {
  const session = await requireSession();
  if (!hasCapability(session, "ManageKnowledgeDocuments")) {
    redirect(
      `/knowledge?error=${encodeURIComponent("Belge yönetme yetkiniz yok.")}`,
    );
  }

  const accessScope = String(formData.get("accessScope") ?? "");
  if (!new Set(["Shared", "Operations", "Management"]).has(accessScope)) {
    redirect(
      `/knowledge?error=${encodeURIComponent("Geçerli bir erişim kapsamı seçin.")}`,
    );
  }

  const response = await fetchServerApi(
    `/api/knowledge/documents/${encodeURIComponent(documentId)}/access-scope`,
    {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ accessScope }),
    },
  );
  if (!response.ok) {
    const problem = (await response.json().catch(() => undefined)) as
      ProblemDetails | undefined;
    redirect(`/knowledge?${problemSearchParams(problem)}`);
  }

  revalidatePath("/knowledge");
  redirect(
    `/knowledge?success=${encodeURIComponent("Belge erişim kapsamı güncellendi.")}`,
  );
}

export async function deleteKnowledgeDocumentAction(documentId: string) {
  const session = await requireSession();
  if (!hasCapability(session, "ManageKnowledgeDocuments")) {
    redirect(
      `/knowledge?error=${encodeURIComponent("Belge yönetme yetkiniz yok.")}`,
    );
  }

  const response = await fetchServerApi(
    `/api/knowledge/documents/${encodeURIComponent(documentId)}`,
    { method: "DELETE" },
  );
  if (!response.ok) {
    const problem = (await response.json().catch(() => undefined)) as
      ProblemDetails | undefined;
    redirect(`/knowledge?${problemSearchParams(problem)}`);
  }

  revalidatePath("/knowledge");
  redirect(
    `/knowledge?success=${encodeURIComponent("Belge bilgi kaynaklarından kaldırıldı.")}`,
  );
}
