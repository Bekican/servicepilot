"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { z } from "zod";

import { problemMessage } from "@/lib/api/problem-details";
import {
  createAnonymousApiClient,
  sessionCookieName,
} from "@/lib/api/server-client";

export type AuthActionState = {
  error?: string;
};

const loginSchema = z.object({
  organizationSlug: z.string().trim().min(1),
  email: z.email(),
  password: z.string().min(8),
});

const registerSchema = z.object({
  organizationName: z.string().trim().min(1).max(200),
  organizationSlug: z
    .string()
    .trim()
    .min(2)
    .max(100)
    .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/),
  firstName: z.string().trim().min(1).max(100),
  lastName: z.string().trim().min(1).max(100),
  email: z.email(),
  password: z.string().min(8).max(128),
  timeZoneId: z.string().trim().min(1),
});

const invitationSchema = z.object({
  token: z.string().min(1),
  firstName: z.string().trim().min(1).max(100),
  lastName: z.string().trim().min(1).max(100),
  password: z.string().min(8).max(128),
});

async function setSession(accessToken: string, expiresAtUtc: string) {
  const cookieStore = await cookies();
  const expires = new Date(expiresAtUtc);

  cookieStore.set(sessionCookieName, accessToken, {
    httpOnly: true,
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
    path: "/",
    expires,
  });
}

export async function loginAction(
  _state: AuthActionState,
  formData: FormData,
): Promise<AuthActionState> {
  const parsed = loginSchema.safeParse(Object.fromEntries(formData));

  if (!parsed.success) {
    return {
      error: "Organizasyon, e-posta ve parola alanlarını kontrol edin.",
    };
  }

  const client = createAnonymousApiClient();
  const { data, error } = await client.POST("/api/auth/login", {
    body: parsed.data,
  });

  if (!data) {
    return { error: problemMessage(error) };
  }

  await setSession(data.accessToken, data.expiresAtUtc);
  redirect("/");
}

export async function registerAction(
  _state: AuthActionState,
  formData: FormData,
): Promise<AuthActionState> {
  const parsed = registerSchema.safeParse(Object.fromEntries(formData));

  if (!parsed.success) {
    return {
      error: "Kayıt bilgilerini ve organizasyon adresini kontrol edin.",
    };
  }

  const client = createAnonymousApiClient();
  const { data, error } = await client.POST("/api/auth/register", {
    body: parsed.data,
  });

  if (!data) {
    return { error: problemMessage(error) };
  }

  await setSession(data.accessToken, data.expiresAtUtc);
  redirect("/");
}

export async function acceptInvitationAction(
  _state: AuthActionState,
  formData: FormData,
): Promise<AuthActionState> {
  const parsed = invitationSchema.safeParse(Object.fromEntries(formData));

  if (!parsed.success) {
    return { error: "Ad, soyad ve en az 8 karakterli parola gereklidir." };
  }

  const client = createAnonymousApiClient();
  const { data, error } = await client.POST("/api/auth/invitations/accept", {
    body: parsed.data,
  });

  if (!data) {
    return {
      error: problemMessage(error, "Davet geçersiz veya süresi dolmuş."),
    };
  }

  await setSession(data.accessToken, data.expiresAtUtc);
  redirect("/");
}

export async function logoutAction() {
  const cookieStore = await cookies();
  cookieStore.delete(sessionCookieName);
  redirect("/login");
}
