"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import { problemMessage } from "@/lib/api/problem-details";
import { createServerApiClient } from "@/lib/api/server-client";
import { formValues, type ActionState } from "@/lib/action-state";

function body(formData: FormData) {
  return {
    name: String(formData.get("name") ?? "").trim(),
    defaultDurationMinutes: Number(formData.get("defaultDurationMinutes")),
  };
}

export async function createServiceAction(
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const client = await createServerApiClient();
  const { data, error } = await client.POST("/api/services", {
    body: body(formData),
  });

  if (!data) {
    return { error: problemMessage(error), values: formValues(formData) };
  }

  revalidatePath("/services");
  return {
    redirectTo: `/services?success=${encodeURIComponent("Hizmet oluşturuldu")}`,
  };
}

export async function updateServiceAction(
  id: string,
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const client = await createServerApiClient();
  const { data, error } = await client.PUT("/api/services/{id}", {
    params: { path: { id } },
    body: body(formData),
  });

  if (!data) {
    return { error: problemMessage(error), values: formValues(formData) };
  }

  revalidatePath("/services");
  return {
    redirectTo: `/services?success=${encodeURIComponent("Hizmet güncellendi")}`,
  };
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
