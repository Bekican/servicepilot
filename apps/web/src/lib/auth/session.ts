import "server-only";

import { cache } from "react";
import { redirect } from "next/navigation";

import { createServerApiClient } from "@/lib/api/server-client";
import type { Session } from "@/lib/api/types";

export const getSession = cache(async (): Promise<Session | null> => {
  const client = await createServerApiClient();
  const { data, response } = await client.GET("/api/auth/me");

  if (!response.ok || !data) {
    return null;
  }

  return data;
});

export async function requireSession() {
  const session = await getSession();

  if (!session) {
    redirect("/login");
  }

  return session;
}

export function hasCapability(session: Session, capability: string) {
  return session.capabilities.includes(capability);
}
