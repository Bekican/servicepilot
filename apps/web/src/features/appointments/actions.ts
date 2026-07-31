"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import { problemMessage } from "@/lib/api/problem-details";
import { createServerApiClient } from "@/lib/api/server-client";

export async function createAppointmentAction(formData: FormData) {
  const client = await createServerApiClient();
  const technicianUserId =
    String(formData.get("technicianUserId") ?? "").trim() || null;
  const { data, error } = await client.POST("/api/appointments", {
    body: {
      customerId: String(formData.get("customerId")),
      serviceId: String(formData.get("serviceId")),
      technicianUserId,
      startAt: String(formData.get("startAt")),
      endAt: String(formData.get("endAt")),
    },
  });

  if (!data) {
    redirect(
      `/appointments/new?error=${encodeURIComponent(problemMessage(error))}`,
    );
  }

  revalidatePath("/appointments");
  revalidatePath("/dashboard");
  redirect(`/appointments/${data.id}?success=Randevu oluşturuldu`);
}

export async function assignTechnicianAction(id: string, formData: FormData) {
  const client = await createServerApiClient();
  const { data, error } = await client.PATCH(
    "/api/appointments/{id}/technician",
    {
      params: { path: { id } },
      body: { technicianUserId: String(formData.get("technicianUserId")) },
    },
  );

  if (!data) {
    redirect(
      `/appointments/${id}?error=${encodeURIComponent(problemMessage(error))}`,
    );
  }

  revalidatePath(`/appointments/${id}`);
  revalidatePath("/appointments");
  revalidatePath("/dashboard");
  redirect(`/appointments/${id}?success=Teknisyen atandı`);
}

export async function transitionAppointmentAction(id: string, status: string) {
  const client = await createServerApiClient();
  const { data, error } = await client.PATCH("/api/appointments/{id}/status", {
    params: { path: { id } },
    body: { status },
  });

  if (!data) {
    redirect(
      `/appointments/${id}?error=${encodeURIComponent(problemMessage(error))}`,
    );
  }

  revalidatePath(`/appointments/${id}`);
  revalidatePath("/appointments");
  revalidatePath("/dashboard");
  redirect(`/appointments/${id}?success=Randevu durumu güncellendi`);
}
