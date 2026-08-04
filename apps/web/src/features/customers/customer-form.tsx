"use client";

import { useActionState, useEffect } from "react";
import { useRouter } from "next/navigation";

import { FormError } from "@/components/shared/form-error";
import { PendingButton } from "@/components/shared/pending-button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { initialActionState, type ActionState } from "@/lib/action-state";
import type { Customer } from "@/lib/api/types";

export function CustomerForm({
  action,
  customer,
  submitLabel,
}: {
  action: (
    state: ActionState,
    formData: FormData,
  ) => ActionState | Promise<ActionState>;
  customer?: Customer;
  submitLabel: string;
}) {
  const [state, formAction] = useActionState(action, initialActionState);
  const router = useRouter();

  useEffect(() => {
    if (state.redirectTo) router.push(state.redirectTo);
  }, [router, state.redirectTo]);

  return (
    <form
      action={formAction}
      aria-describedby={state.error ? "customer-form-error" : undefined}
      className="space-y-7"
      key={state.values ? JSON.stringify(state.values) : "initial"}
    >
      <FormError
        id="customer-form-error"
        message={state.error}
        supportCode={state.supportCode}
      />
      <div className="space-y-2">
        <Label htmlFor="type">Müşteri türü</Label>
        <select
          className="bg-background h-10 w-full rounded-md border px-3 text-sm"
          defaultValue={state.values?.type ?? customer?.type ?? "Individual"}
          id="type"
          name="type"
        >
          <option value="Individual">Bireysel</option>
          <option value="Company">Kurumsal</option>
        </select>
        <p className="text-muted-foreground text-xs">
          Bireysel müşteri için ad/soyad, kurumsal müşteri için şirket adı
          zorunludur.
        </p>
      </div>

      <div className="grid gap-5 sm:grid-cols-2">
        <FormField
          defaultValue={state.values?.firstName ?? customer?.firstName ?? ""}
          label="Ad"
          name="firstName"
        />
        <FormField
          defaultValue={state.values?.lastName ?? customer?.lastName ?? ""}
          label="Soyad"
          name="lastName"
        />
        <FormField
          defaultValue={
            state.values?.companyName ?? customer?.companyName ?? ""
          }
          label="Şirket adı"
          name="companyName"
        />
        <FormField
          defaultValue={
            state.values?.contactPerson ?? customer?.contactPerson ?? ""
          }
          label="İletişim kişisi"
          name="contactPerson"
        />
        <FormField
          defaultValue={state.values?.email ?? customer?.email ?? ""}
          label="E-posta"
          name="email"
          type="email"
        />
        <FormField
          defaultValue={state.values?.phone ?? customer?.phone ?? ""}
          label="Telefon"
          name="phone"
          placeholder="+905551112233"
        />
      </div>

      <div className="flex justify-end">
        <PendingButton pendingLabel="Kaydediliyor…" type="submit">
          {submitLabel}
        </PendingButton>
      </div>
    </form>
  );
}

function FormField({
  label,
  name,
  ...props
}: React.ComponentProps<typeof Input> & { label: string; name: string }) {
  return (
    <div className="space-y-2">
      <Label htmlFor={name}>{label}</Label>
      <Input id={name} name={name} {...props} />
    </div>
  );
}
