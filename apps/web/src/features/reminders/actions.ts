"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import { problemSearchParams } from "@/lib/api/problem-details";
import { createServerApiClient } from "@/lib/api/server-client";

export async function retryReminderAction(id: string) {
  const client = await createServerApiClient();
  const { data, error } = await client.POST("/api/reminders/{id}/retry", {
    params: { path: { id } },
  });

  if (!data) {
    redirect(`/reminders?${problemSearchParams(error)}`);
  }

  revalidatePath("/reminders");
  revalidatePath("/dashboard");
  redirect(
    `/reminders?success=${encodeURIComponent("Hatırlatma yeniden kuyruğa alındı")}`,
  );
}
