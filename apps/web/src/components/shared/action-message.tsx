import { AlertCircle, CheckCircle2 } from "lucide-react";

export function ActionMessage({
  error,
  success,
}: {
  error?: string;
  success?: string;
}) {
  if (!error && !success) return null;

  return (
    <div
      className={
        error
          ? "border-destructive/20 bg-destructive/5 text-destructive mb-6 flex gap-2 rounded-lg border p-3 text-sm"
          : "mb-6 flex gap-2 rounded-lg border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-800"
      }
      role="status"
    >
      {error ? (
        <AlertCircle className="size-4" />
      ) : (
        <CheckCircle2 className="size-4" />
      )}
      {error ?? success}
    </div>
  );
}
