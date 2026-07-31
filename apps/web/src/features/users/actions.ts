"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import { problemMessage } from "@/lib/api/problem-details";
import { createServerApiClient } from "@/lib/api/server-client";
import { formValues, type ActionState } from "@/lib/action-state";

export async function createInvitationAction(
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const client = await createServerApiClient();
  const { data, error } = await client.POST("/api/users/invitations", {
    body: {
      email: String(formData.get("email") ?? "").trim(),
      role: String(formData.get("role") ?? "Technician"),
    },
  });

  if (!data) {
    return { error: problemMessage(error), values: formValues(formData) };
  }

  revalidatePath("/users");
  return {
    redirectTo: `/users?success=${encodeURIComponent("Davet e-postası gönderildi")}`,
  };
}

export async function resendInvitationAction(id: string) {
  const client = await createServerApiClient();
  const { data, error } = await client.POST(
    "/api/users/invitations/{id}/resend",
    {
      params: { path: { id } },
    },
  );

  if (!data) {
    redirect(`/users?error=${encodeURIComponent(problemMessage(error))}`);
  }

  revalidatePath("/users");
  redirect(`/users?success=${encodeURIComponent("Davet yeniden gönderildi")}`);
}

export async function changeUserRoleAction(id: string, formData: FormData) {
  const client = await createServerApiClient();
  const { data, error } = await client.PATCH("/api/users/{id}/role", {
    params: { path: { id } },
    body: { role: String(formData.get("role") ?? "") },
  });

  if (!data) {
    redirect(`/users?error=${encodeURIComponent(problemMessage(error))}`);
  }

  revalidatePath("/users");
}

export async function changeUserStatusAction(id: string, isActive: boolean) {
  const client = await createServerApiClient();
  const { data, error } = await client.PATCH("/api/users/{id}/status", {
    params: { path: { id } },
    body: { isActive },
  });

  if (!data) {
    redirect(`/users?error=${encodeURIComponent(problemMessage(error))}`);
  }

  revalidatePath("/users");
}
