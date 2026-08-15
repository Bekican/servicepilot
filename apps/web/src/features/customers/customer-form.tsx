"use client";

import { useActionState, useEffect, useState } from "react";
import { useRouter } from "next/navigation";

import { FormError } from "@/components/shared/form-error";
import { FieldError } from "@/components/shared/field-error";
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
  const [customerType, setCustomerType] = useState(
    state.values?.type ?? customer?.type ?? "Individual",
  );

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
          aria-invalid={Boolean(state.fieldErrors?.type)}
          className="bg-background h-10 w-full rounded-md border px-3 text-sm"
          onChange={(event) => setCustomerType(event.target.value)}
          id="type"
          name="type"
          value={customerType}
        >
          <option value="Individual">Bireysel</option>
          <option value="Company">Kurumsal</option>
        </select>
        <FieldError id="type-error" message={state.fieldErrors?.type} />
        <p className="text-muted-foreground text-xs">
          Bireysel müşteri için ad/soyad, kurumsal müşteri için şirket adı
          zorunludur.
        </p>
      </div>

      <div className="grid gap-5 sm:grid-cols-2">
        {customerType === "Individual" ? (
          <>
            <FormField
              defaultValue={
                state.values?.firstName ?? customer?.firstName ?? ""
              }
              error={state.fieldErrors?.firstName}
              label="Ad"
              maxLength={100}
              name="firstName"
              required
            />
            <FormField
              defaultValue={state.values?.lastName ?? customer?.lastName ?? ""}
              error={state.fieldErrors?.lastName}
              label="Soyad"
              maxLength={100}
              name="lastName"
              required
            />
          </>
        ) : (
          <>
            <FormField
              defaultValue={
                state.values?.companyName ?? customer?.companyName ?? ""
              }
              error={state.fieldErrors?.companyName}
              label="Şirket adı"
              maxLength={200}
              name="companyName"
              required
            />
            <FormField
              defaultValue={
                state.values?.contactPerson ?? customer?.contactPerson ?? ""
              }
              error={state.fieldErrors?.contactPerson}
              label="İletişim kişisi (opsiyonel)"
              maxLength={200}
              name="contactPerson"
            />
          </>
        )}
        <FormField
          defaultValue={state.values?.email ?? customer?.email ?? ""}
          error={state.fieldErrors?.email}
          label="E-posta (opsiyonel)"
          maxLength={320}
          name="email"
          type="email"
        />
        <FormField
          defaultValue={state.values?.phone ?? customer?.phone ?? ""}
          error={state.fieldErrors?.phone}
          label="Telefon (opsiyonel)"
          name="phone"
          placeholder="0555 111 22 33 veya +90 555 111 22 33"
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
  error,
  label,
  name,
  ...props
}: React.ComponentProps<typeof Input> & {
  error?: string;
  label: string;
  name: string;
}) {
  const errorId = `${name}-error`;
  return (
    <div className="space-y-2">
      <Label htmlFor={name}>{label}</Label>
      <Input
        aria-describedby={error ? errorId : undefined}
        aria-invalid={Boolean(error)}
        id={name}
        name={name}
        {...props}
      />
      <FieldError id={errorId} message={error} />
    </div>
  );
}
