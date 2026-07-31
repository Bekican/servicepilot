"use client";

import { useActionState, useEffect } from "react";
import { useRouter } from "next/navigation";

import { FormError } from "@/components/shared/form-error";
import { PendingButton } from "@/components/shared/pending-button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { initialActionState } from "@/lib/action-state";

import { createInvitationAction } from "./actions";

const roles = ["Owner", "Admin", "Dispatcher", "Technician"];

export function InvitationForm() {
  const [state, action] = useActionState(
    createInvitationAction,
    initialActionState,
  );
  const router = useRouter();

  useEffect(() => {
    if (state.redirectTo) router.push(state.redirectTo);
  }, [router, state.redirectTo]);

  return (
    <form
      action={action}
      aria-describedby={state.error ? "invitation-form-error" : undefined}
      className="space-y-4"
      key={state.values ? JSON.stringify(state.values) : "initial"}
    >
      <FormError id="invitation-form-error" message={state.error} />
      <div className="space-y-2">
        <Label htmlFor="invitation-email">E-posta</Label>
        <Input
          autoComplete="email"
          defaultValue={state.values?.email}
          id="invitation-email"
          name="email"
          required
          type="email"
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="invitation-role">Başlangıç rolü</Label>
        <select
          className="bg-background h-10 w-full rounded-md border px-3 text-sm"
          defaultValue={state.values?.role ?? "Technician"}
          id="invitation-role"
          name="role"
        >
          {roles.map((role) => (
            <option key={role}>{role}</option>
          ))}
        </select>
      </div>
      <PendingButton
        className="w-full"
        pendingLabel="Gönderiliyor…"
        type="submit"
      >
        Davet gönder
      </PendingButton>
    </form>
  );
}
