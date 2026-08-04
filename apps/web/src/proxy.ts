import type { NextRequest } from "next/server";
import { NextResponse } from "next/server";

import {
  correlationIdHeader,
  resolveCorrelationId,
} from "@/lib/observability/correlation-id";

export function proxy(request: NextRequest) {
  const correlationId = resolveCorrelationId(
    request.headers.get(correlationIdHeader),
  );
  const requestHeaders = new Headers(request.headers);
  requestHeaders.set(correlationIdHeader, correlationId);

  const response = NextResponse.next({
    request: {
      headers: requestHeaders,
    },
  });
  response.headers.set(correlationIdHeader, correlationId);

  return response;
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
