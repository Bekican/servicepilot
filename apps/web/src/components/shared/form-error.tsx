import { AlertCircle } from "lucide-react";

export function FormError({ id, message }: { id?: string; message?: string }) {
  if (!message) return null;

  return (
    <div
      aria-live="assertive"
      className="border-destructive/25 bg-destructive/5 text-destructive flex gap-2 rounded-lg border p-3 text-sm"
      id={id}
      role="alert"
    >
      <AlertCircle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
      <span>{message}</span>
    </div>
  );
}
