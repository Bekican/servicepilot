"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { z } from "zod";

import { createServerApiClient } from "@/lib/api/server-client";
import { problemMessage } from "@/lib/api/problem-details";
import { formValues, type ActionState } from "@/lib/action-state";

const optional = (value: FormDataEntryValue | null) => {
  const normalized = String(value ?? "").trim();
  return normalized || null;
};

const customerSchema = z.object({
  type: z.enum(["Individual", "Company"]),
  firstName: z.string(),
  lastName: z.string(),
  companyName: z.string(),
  contactPerson: z.string(),
  email: z.string(),
  phone: z.string(),
});

function customerBody(formData: FormData) {
  const parsed = customerSchema.parse({
    type: formData.get("type"),
    firstName: String(formData.get("firstName") ?? ""),
    lastName: String(formData.get("lastName") ?? ""),
    companyName: String(formData.get("companyName") ?? ""),
    contactPerson: String(formData.get("contactPerson") ?? ""),
    email: String(formData.get("email") ?? ""),
    phone: String(formData.get("phone") ?? ""),
  });

  return {
    type: parsed.type,
    firstName: optional(parsed.firstName),
    lastName: optional(parsed.lastName),
    companyName: optional(parsed.companyName),
    contactPerson: optional(parsed.contactPerson),
    email: optional(parsed.email),
    phone: optional(parsed.phone),
  };
}

export async function createCustomerAction(
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const client = await createServerApiClient();
  const { data, error } = await client.POST("/api/customers", {
    body: customerBody(formData),
  });

  if (!data) {
    return { error: problemMessage(error), values: formValues(formData) };
  }

  revalidatePath("/customers");
  return {
    redirectTo: `/customers/${data.id}?success=${encodeURIComponent("Müşteri oluşturuldu")}`,
  };
}

export async function updateCustomerAction(
  id: string,
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const client = await createServerApiClient();
  const { data, error } = await client.PUT("/api/customers/{id}", {
    params: { path: { id } },
    body: customerBody(formData),
  });

  if (!data) {
    return { error: problemMessage(error), values: formValues(formData) };
  }

  revalidatePath("/customers");
  revalidatePath(`/customers/${id}`);
  return {
    redirectTo: `/customers/${id}?success=${encodeURIComponent("Müşteri güncellendi")}`,
  };
}

export async function deactivateCustomerAction(id: string) {
  const client = await createServerApiClient();
  const { response, error } = await client.POST(
    "/api/customers/{id}/deactivate",
    {
      params: { path: { id } },
    },
  );

  if (!response.ok) {
    redirect(
      `/customers/${id}?error=${encodeURIComponent(problemMessage(error))}`,
    );
  }

  revalidatePath("/customers");
  revalidatePath(`/customers/${id}`);
  redirect(
    `/customers/${id}?success=${encodeURIComponent("Müşteri pasifleştirildi")}`,
  );
}

export async function addAddressAction(
  customerId: string,
  _state: ActionState,
  formData: FormData,
): Promise<ActionState> {
  const client = await createServerApiClient();
  const { data, error } = await client.POST(
    "/api/customers/{customerId}/addresses",
    {
      params: { path: { customerId } },
      body: {
        label: optional(formData.get("label")),
        line1: String(formData.get("line1") ?? ""),
        line2: optional(formData.get("line2")),
        city: String(formData.get("city") ?? ""),
        region: optional(formData.get("region")),
        postalCode: optional(formData.get("postalCode")),
        countryCode: String(formData.get("countryCode") ?? "TR").toUpperCase(),
        isPrimary: formData.get("isPrimary") === "on",
      },
    },
  );

  if (!data) {
    return { error: problemMessage(error), values: formValues(formData) };
  }

  revalidatePath(`/customers/${customerId}`);
  return {
    redirectTo: `/customers/${customerId}?success=${encodeURIComponent("Adres eklendi")}`,
  };
}

export async function setPrimaryAddressAction(
  customerId: string,
  addressId: string,
) {
  const client = await createServerApiClient();
  const { data, error } = await client.PUT(
    "/api/customers/{customerId}/addresses/{addressId}/primary",
    { params: { path: { customerId, addressId } } },
  );

  if (!data) {
    redirect(
      `/customers/${customerId}?error=${encodeURIComponent(problemMessage(error))}`,
    );
  }

  revalidatePath(`/customers/${customerId}`);
}

export async function deactivateAddressAction(
  customerId: string,
  addressId: string,
) {
  const client = await createServerApiClient();
  const { response, error } = await client.POST(
    "/api/customers/{customerId}/addresses/{addressId}/deactivate",
    { params: { path: { customerId, addressId } } },
  );

  if (!response.ok) {
    redirect(
      `/customers/${customerId}?error=${encodeURIComponent(problemMessage(error))}`,
    );
  }

  revalidatePath(`/customers/${customerId}`);
}
