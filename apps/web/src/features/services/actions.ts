"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import { problemMessage } from "@/lib/api/problem-details";
import { createServerApiClient } from "@/lib/api/server-client";

function body(formData: FormData) {
  return {
    name: String(formData.get("name") ?? "").trim(),
    defaultDurationMinutes: Number(formData.get("defaultDurationMinutes")),
  };
}

export async function createServiceAction(formData: FormData) {
  const client = await createServerApiClient();
  const { data, error } = await client.POST("/api/services", {
    body: body(formData),
  });

  if (!data) {
    redirect(`/services?error=${encodeURIComponent(problemMessage(error))}`);
  }

  revalidatePath("/services");
  redirect("/services?success=Hizmet oluşturuldu");
}

export async function updateServiceAction(id: string, formData: FormData) {
  const client = await createServerApiClient();
  const { data, error } = await client.PUT("/api/services/{id}", {
    params: { path: { id } },
    body: body(formData),
  });

  if (!data) {
    redirect(`/services?error=${encodeURIComponent(problemMessage(error))}`);
  }

  revalidatePath("/services");
  redirect("/services?success=Hizmet güncellendi");
}

export async function setServiceStatusAction(id: string, isActive: boolean) {
  const client = await createServerApiClient();
  const { data, error } = await client.PATCH("/api/services/{id}/status", {
    params: { path: { id } },
    body: { isActive },
  });

  if (!data) {
    redirect(`/services?error=${encodeURIComponent(problemMessage(error))}`);
  }

  revalidatePath("/services");
}
