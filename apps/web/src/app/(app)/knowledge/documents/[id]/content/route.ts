import type { NextRequest } from "next/server";

import { fetchServerApi } from "@/lib/api/server-client";

export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ id: string }> },
) {
  const { id } = await params;
  const range = request.headers.get("range");
  const response = await fetchServerApi(
    `/api/knowledge/documents/${encodeURIComponent(id)}/content`,
    { headers: range ? { Range: range } : undefined },
  );
  const headers = new Headers();
  for (const name of [
    "accept-ranges",
    "content-disposition",
    "content-length",
    "content-range",
    "content-type",
  ]) {
    const value = response.headers.get(name);
    if (value) headers.set(name, value);
  }
  headers.set("Cache-Control", "private, no-store");

  return new Response(response.body, {
    status: response.status,
    headers,
  });
}
