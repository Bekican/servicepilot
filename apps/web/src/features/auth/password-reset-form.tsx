"use client";

import { useActionState } from "react";
import { FormError } from "@/components/shared/form-error";
import { FieldError } from "@/components/shared/field-error";
import { PendingButton } from "@/components/shared/pending-button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  requestPasswordResetAction,
  completePasswordResetAction,
} from "./password-reset-actions";

export function PasswordResetRequestForm() {
  const [state, action] = useActionState(requestPasswordResetAction, {});
  return (
    <form action={action} className="space-y-5">
      <FormError message={state.error} supportCode={state.supportCode} />
      <div className="space-y-2">
        <Label htmlFor="organizationSlug">Organizasyon kısa adı</Label>
        <Input
          aria-invalid={Boolean(state.fieldErrors?.organizationSlug)}
          id="organizationSlug"
          name="organizationSlug"
          required
        />
        <FieldError
          id="organizationSlug-error"
          message={state.fieldErrors?.organizationSlug}
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="email">E-posta</Label>
        <Input
          aria-invalid={Boolean(state.fieldErrors?.email)}
          id="email"
          name="email"
          type="email"
          required
        />
        <FieldError id="email-error" message={state.fieldErrors?.email} />
      </div>
      <PendingButton
        className="w-full"
        pendingLabel="Gönderiliyor…"
        type="submit"
      >
        Bağlantı gönder
      </PendingButton>
    </form>
  );
}

export function PasswordResetCompleteForm({ token }: { token: string }) {
  const [state, action] = useActionState(completePasswordResetAction, {});
  return (
    <form action={action} className="space-y-5">
      <FormError message={state.error} supportCode={state.supportCode} />
      <input name="token" type="hidden" value={token} />
      <div className="space-y-2">
        <Label htmlFor="password">Yeni parola</Label>
        <Input
          aria-invalid={Boolean(state.fieldErrors?.password)}
          id="password"
          minLength={8}
          maxLength={128}
          name="password"
          type="password"
          required
        />
        <FieldError id="password-error" message={state.fieldErrors?.password} />
      </div>
      <PendingButton
        className="w-full"
        pendingLabel="Yenileniyor…"
        type="submit"
      >
        Parolayı yenile
      </PendingButton>
    </form>
  );
}
