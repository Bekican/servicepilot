import "server-only";

import { cookies } from "next/headers";
import createClient from "openapi-fetch";

import type { paths } from "@/lib/api/generated/schema";

export const sessionCookieName = "servicepilot_session";

function apiBaseUrl() {
  return (
    process.env.SERVICEPILOT_API_URL?.replace(/\/$/, "") ??
    "http://127.0.0.1:5267"
  );
}

export function createAnonymousApiClient() {
  return createClient<paths>({
    baseUrl: apiBaseUrl(),
  });
}

export async function createServerApiClient() {
  const cookieStore = await cookies();
  const token = cookieStore.get(sessionCookieName)?.value;

  return createClient<paths>({
    baseUrl: apiBaseUrl(),
    headers: token
      ? {
          Authorization: `Bearer ${token}`,
        }
      : undefined,
  });
}
