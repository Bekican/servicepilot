import { AlertCircle } from "lucide-react";

import { SupportCode } from "@/components/shared/support-code";

export function FormError({
  id,
  message,
  supportCode,
}: {
  id?: string;
  message?: string;
  supportCode?: string;
}) {
  if (!message) return null;

  return (
    <div
      aria-live="assertive"
      className="border-destructive/25 bg-destructive/5 text-destructive flex gap-2 rounded-lg border p-3 text-sm"
      id={id}
      role="alert"
    >
      <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
      <span>
        <span>{message}</span>
        <SupportCode value={supportCode} />
      </span>
    </div>
  );
}
