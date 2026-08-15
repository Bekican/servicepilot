"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { z } from "zod";

import {
  problemActionState,
  problemPresentation,
} from "@/lib/api/problem-details";
import {
  createAnonymousApiClient,
  sessionCookieName,
} from "@/lib/api/server-client";

export type AuthActionState = {
  error?: string;
  supportCode?: string;
  fieldErrors?: Record<string, string>;
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

  const client = await createAnonymousApiClient();
  const { data, error } = await client.POST("/api/auth/login", {
    body: parsed.data,
  });

  if (!data) {
    const problem = problemPresentation(error);
    return { error: problem.message, supportCode: problem.supportCode };
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
      fieldErrors: Object.fromEntries(
        Object.keys(parsed.error.flatten().fieldErrors).map((field) => [
          field,
          "Bu alanı kontrol edin.",
        ]),
      ),
    };
  }

  const client = await createAnonymousApiClient();
  const { data, error } = await client.POST("/api/auth/register", {
    body: parsed.data,
  });

  if (!data) {
    return problemActionState(error);
  }

  await setSession(data.accessToken, data.expiresAtUtc);
  redirect("/dashboard");
}

export async function acceptInvitationAction(
  _state: AuthActionState,
  formData: FormData,
): Promise<AuthActionState> {
  const parsed = invitationSchema.safeParse(Object.fromEntries(formData));

  if (!parsed.success) {
    return { error: "Ad, soyad ve en az 8 karakterli parola gereklidir." };
  }

  const client = await createAnonymousApiClient();
  const { data, error } = await client.POST("/api/auth/invitations/accept", {
    body: parsed.data,
  });

  if (!data) {
    const problem = problemPresentation(
      error,
      "Davet geçersiz veya süresi dolmuş.",
    );
    return { error: problem.message, supportCode: problem.supportCode };
  }

  await setSession(data.accessToken, data.expiresAtUtc);
  redirect("/appointments");
}

export async function logoutAction() {
  const cookieStore = await cookies();
  cookieStore.delete(sessionCookieName);
  redirect("/login");
}
