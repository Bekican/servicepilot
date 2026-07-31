"use client";

import { useActionState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Clock3 } from "lucide-react";

import { FormError } from "@/components/shared/form-error";
import { PendingButton } from "@/components/shared/pending-button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { initialActionState, type ActionState } from "@/lib/action-state";

export function ServiceForm({
  action,
  defaultDurationMinutes = 60,
  defaultName,
  disabled,
  idSuffix = "new",
  mode,
}: {
  action: (
    state: ActionState,
    formData: FormData,
  ) => ActionState | Promise<ActionState>;
  defaultDurationMinutes?: number;
  defaultName?: string;
  disabled?: boolean;
  idSuffix?: string;
  mode: "create" | "update";
}) {
  const [state, formAction] = useActionState(action, initialActionState);
  const router = useRouter();
  const errorId = `service-form-error-${idSuffix}`;

  useEffect(() => {
    if (state.redirectTo) router.push(state.redirectTo);
  }, [router, state.redirectTo]);

  return (
    <form
      action={formAction}
      aria-describedby={state.error ? errorId : undefined}
      className={
        mode === "update"
          ? "grid items-end gap-4 sm:grid-cols-[minmax(0,1fr)_180px_auto]"
          : "space-y-5"
      }
      key={state.values ? JSON.stringify(state.values) : "initial"}
    >
      <div className={mode === "update" ? "sm:col-span-3" : undefined}>
        <FormError id={errorId} message={state.error} />
      </div>
      <div className="space-y-2">
        <Label htmlFor={`name-${idSuffix}`}>Hizmet adı</Label>
        <Input
          defaultValue={state.values?.name ?? defaultName}
          disabled={disabled}
          id={`name-${idSuffix}`}
          name="name"
          placeholder={mode === "create" ? "Kombi Bakımı" : undefined}
          required
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor={`duration-${idSuffix}`}>
          {mode === "create" ? "Varsayılan süre" : "Süre (dakika)"}
        </Label>
        <div className="relative">
          {mode === "create" ? (
            <Clock3
              aria-hidden="true"
              className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2"
            />
          ) : null}
          <Input
            className={mode === "create" ? "pl-9" : undefined}
            defaultValue={
              state.values?.defaultDurationMinutes ?? defaultDurationMinutes
            }
            disabled={disabled}
            id={`duration-${idSuffix}`}
            min={5}
            name="defaultDurationMinutes"
            required
            type="number"
          />
        </div>
      </div>
      <PendingButton
        className={mode === "create" ? "w-full" : undefined}
        disabled={disabled}
        pendingLabel={mode === "create" ? "Oluşturuluyor…" : "Kaydediliyor…"}
        type="submit"
        variant={mode === "update" ? "outline" : "default"}
      >
        {mode === "create" ? "Hizmeti oluştur" : "Kaydet"}
      </PendingButton>
    </form>
  );
}
