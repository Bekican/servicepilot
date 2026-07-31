"use client";

import { useActionState, useEffect } from "react";
import { useRouter } from "next/navigation";

import { FormError } from "@/components/shared/form-error";
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
      <FormError id="address-form-error" message={state.error} />
      <AddressField
        defaultValue={state.values?.label}
        label="Etiket"
        name="label"
        placeholder="Ev, İş..."
      />
      <AddressField
        defaultValue={state.values?.line1}
        label="Adres satırı"
        name="line1"
        required
      />
      <AddressField
        defaultValue={state.values?.line2}
        label="Adres satırı 2"
        name="line2"
      />
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <AddressField
          defaultValue={state.values?.city}
          label="Şehir"
          name="city"
          required
        />
        <AddressField
          defaultValue={state.values?.region}
          label="Bölge"
          name="region"
        />
        <AddressField
          defaultValue={state.values?.postalCode}
          label="Posta kodu"
          name="postalCode"
        />
        <AddressField
          defaultValue={state.values?.countryCode ?? "TR"}
          label="Ülke kodu"
          maxLength={2}
          name="countryCode"
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
  label,
  name,
  ...props
}: React.ComponentProps<typeof Input> & { label: string; name: string }) {
  return (
    <div className="space-y-2">
      <Label htmlFor={`address-${name}`}>{label}</Label>
      <Input id={`address-${name}`} name={name} {...props} />
    </div>
  );
}
