import "server-only";

import { cookies, headers } from "next/headers";
import createClient from "openapi-fetch";

import type { paths } from "@/lib/api/generated/schema";
import {
  correlationIdHeader,
  resolveCorrelationId,
} from "@/lib/observability/correlation-id";

export const sessionCookieName = "servicepilot_session";

function apiBaseUrl() {
  return (
    process.env.SERVICEPILOT_API_URL?.replace(/\/$/, "") ??
    "http://127.0.0.1:5267"
  );
}

async function requestCorrelationId() {
  const requestHeaders = await headers();
  return resolveCorrelationId(requestHeaders.get(correlationIdHeader));
}

export async function createAnonymousApiClient() {
  return createClient<paths>({
    baseUrl: apiBaseUrl(),
    headers: {
      [correlationIdHeader]: await requestCorrelationId(),
    },
  });
}

export async function createServerApiClient() {
  const cookieStore = await cookies();
  const token = cookieStore.get(sessionCookieName)?.value;
  const correlationId = await requestCorrelationId();

  return createClient<paths>({
    baseUrl: apiBaseUrl(),
    headers: {
      [correlationIdHeader]: correlationId,
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });
}
