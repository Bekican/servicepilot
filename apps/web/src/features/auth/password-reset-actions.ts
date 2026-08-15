"use server";

import { redirect } from "next/navigation";

import { createAnonymousApiClient } from "@/lib/api/server-client";
import { problemActionState } from "@/lib/api/problem-details";
import type { AuthActionState } from "@/lib/auth/actions";

export async function requestPasswordResetAction(
  _state: AuthActionState,
  formData: FormData,
): Promise<AuthActionState> {
  const client = await createAnonymousApiClient();
  const { response, error } = await client.POST(
    "/api/auth/password-reset/request",
    {
      body: {
        organizationSlug: String(formData.get("organizationSlug") ?? "").trim(),
        email: String(formData.get("email") ?? "").trim(),
      },
    },
  );
  if (!response.ok) return problemActionState(error);
  redirect(
    `/login?success=${encodeURIComponent("Bilgiler eşleşiyorsa parola yenileme bağlantısı e-posta adresinize gönderildi.")}`,
  );
}

export async function completePasswordResetAction(
  _state: AuthActionState,
  formData: FormData,
): Promise<AuthActionState> {
  const client = await createAnonymousApiClient();
  const { response, error } = await client.POST(
    "/api/auth/password-reset/complete",
    {
      body: {
        token: String(formData.get("token") ?? ""),
        password: String(formData.get("password") ?? ""),
      },
    },
  );
  if (!response.ok) return problemActionState(error);
  redirect(
    `/login?success=${encodeURIComponent("Parolanız yenilendi. Yeni parolanızla giriş yapabilirsiniz.")}`,
  );
}
