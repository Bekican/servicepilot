"use client";

import { useActionState, useEffect } from "react";
import { useRouter } from "next/navigation";

import { FormError } from "@/components/shared/form-error";
import { FieldError } from "@/components/shared/field-error";
import { PendingButton } from "@/components/shared/pending-button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { initialActionState } from "@/lib/action-state";

import { addAddressAction } from "./actions";

export function AddressForm({ customerId }: { customerId: string }) {
  const [state, action] = useActionState(
    addAddressAction.bind(null, customerId),
    initialActionState,
  );
  const router = useRouter();

  useEffect(() => {
    if (state.redirectTo) router.push(state.redirectTo);
  }, [router, state.redirectTo]);

  return (
    <form
      action={action}
      aria-describedby={state.error ? "address-form-error" : undefined}
      className="space-y-4"
      key={state.values ? JSON.stringify(state.values) : "initial"}
    >
      <FormError
        id="address-form-error"
        message={state.error}
        supportCode={state.supportCode}
      />
      <AddressField
        defaultValue={state.values?.label}
        label="Etiket"
        name="label"
        error={state.fieldErrors?.label}
        placeholder="Ev, İş..."
      />
      <AddressField
        defaultValue={state.values?.line1}
        label="Adres satırı"
        name="line1"
        error={state.fieldErrors?.line1}
        required
      />
      <AddressField
        defaultValue={state.values?.line2}
        label="Adres satırı 2"
        name="line2"
        error={state.fieldErrors?.line2}
      />
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <AddressField
          defaultValue={state.values?.city}
          label="Şehir"
          name="city"
          error={state.fieldErrors?.city}
          required
        />
        <AddressField
          defaultValue={state.values?.region}
          label="Bölge"
          name="region"
          error={state.fieldErrors?.region}
        />
        <AddressField
          defaultValue={state.values?.postalCode}
          label="Posta kodu"
          name="postalCode"
          error={state.fieldErrors?.postalCode}
        />
        <AddressField
          defaultValue={state.values?.countryCode ?? "TR"}
          label="Ülke kodu"
          maxLength={2}
          name="countryCode"
          error={state.fieldErrors?.countryCode}
          required
        />
      </div>
      <label className="flex items-center gap-2 text-sm">
        <input
          defaultChecked={state.values?.isPrimary === "on"}
          name="isPrimary"
          type="checkbox"
        />
        Birincil adres yap
      </label>
      <PendingButton pendingLabel="Ekleniyor…" type="submit">
        Adresi ekle
      </PendingButton>
    </form>
  );
}

function AddressField({
  error,
  label,
  name,
  ...props
}: React.ComponentProps<typeof Input> & {
  error?: string;
  label: string;
  name: string;
}) {
  const errorId = `address-${name}-error`;
  return (
    <div className="space-y-2">
      <Label htmlFor={`address-${name}`}>{label}</Label>
      <Input
        aria-describedby={error ? errorId : undefined}
        aria-invalid={Boolean(error)}
        id={`address-${name}`}
        name={name}
        {...props}
      />
      <FieldError id={errorId} message={error} />
    </div>
  );
}
