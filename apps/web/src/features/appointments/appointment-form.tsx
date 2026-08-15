"use client";

import { useActionState, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { CalendarClock } from "lucide-react";

import { FormError } from "@/components/shared/form-error";
import { FieldError } from "@/components/shared/field-error";
import { PendingButton } from "@/components/shared/pending-button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { Customer, Service, Technician } from "@/lib/api/types";
import { localDateTimeValue, zonedLocalDateTimeToIso } from "@/lib/date";
import { initialActionState } from "@/lib/action-state";

import { createAppointmentAction } from "./actions";

function customerName(customer: Customer) {
  return customer.type === "Company"
    ? customer.companyName
    : `${customer.firstName ?? ""} ${customer.lastName ?? ""}`.trim();
}

function initialDateTime(timeZone: string) {
  const value = new Date(Date.now() + 60 * 60 * 1000);
  value.setMinutes(Math.ceil(value.getMinutes() / 15) * 15, 0, 0);
  return localDateTimeValue(value, timeZone);
}

export function AppointmentForm({
  customers,
  services,
  technicians,
  timeZone,
}: {
  customers: Customer[];
  services: Service[];
  technicians: Technician[];
  timeZone: string;
}) {
  const [state, formAction] = useActionState(
    createAppointmentAction,
    initialActionState,
  );
  const router = useRouter();

  useEffect(() => {
    if (state.redirectTo) router.push(state.redirectTo);
  }, [router, state.redirectTo]);
  const [customerId, setCustomerId] = useState(
    state.values?.customerId ?? customers[0]?.id ?? "",
  );
  const [serviceId, setServiceId] = useState(
    state.values?.serviceId ?? services[0]?.id ?? "",
  );
  const [startLocal, setStartLocal] = useState(
    state.values?.startLocal ?? initialDateTime(timeZone),
  );
  const customer = customers.find((item) => item.id === customerId);
  const service = services.find((item) => item.id === serviceId);
  const duration = Number(service?.defaultDurationMinutes ?? 60);
  const conversion = useMemo(() => {
    if (!startLocal) return { instants: null, error: undefined };
    try {
      const startAt = zonedLocalDateTimeToIso(startLocal, timeZone);
      const endAt = new Date(
        new Date(startAt).getTime() + duration * 60_000,
      ).toISOString();
      return { instants: { startAt, endAt }, error: undefined };
    } catch (error) {
      return {
        instants: null,
        error:
          error instanceof Error
            ? error.message
            : "Geçerli bir tarih ve saat seçin.",
      };
    }
  }, [duration, startLocal, timeZone]);
  const { instants } = conversion;

  return (
    <form
      action={formAction}
      aria-describedby={state.error ? "appointment-form-error" : undefined}
      className="space-y-6"
      key={state.values ? JSON.stringify(state.values) : "initial"}
    >
      <FormError
        id="appointment-form-error"
        message={state.error}
        supportCode={state.supportCode}
      />
      <input name="startAt" type="hidden" value={instants?.startAt ?? ""} />
      <input name="endAt" type="hidden" value={instants?.endAt ?? ""} />

      <div className="space-y-2">
        <Label htmlFor="customerId">Müşteri</Label>
        <select
          className="bg-background h-10 w-full rounded-md border px-3 text-sm"
          id="customerId"
          name="customerId"
          onChange={(event) => setCustomerId(event.target.value)}
          required
          value={customerId}
        >
          {customers.map((customer) => (
            <option key={customer.id} value={customer.id}>
              {customer.customerNumber} · {customerName(customer)}
            </option>
          ))}
        </select>
      </div>

      <div className="space-y-2">
        <Label htmlFor="serviceId">Hizmet</Label>
        <select
          className="bg-background h-10 w-full rounded-md border px-3 text-sm"
          id="serviceId"
          name="serviceId"
          onChange={(event) => setServiceId(event.target.value)}
          required
          value={serviceId}
        >
          {services.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name} · {Number(item.defaultDurationMinutes)} dakika
            </option>
          ))}
        </select>
      </div>

      <div className="space-y-2">
        <Label htmlFor="startLocal">Başlangıç</Label>
        <Input
          aria-invalid={Boolean(state.fieldErrors?.startAt)}
          id="startLocal"
          min={localDateTimeValue(new Date(), timeZone)}
          name="startLocal"
          onChange={(event) => setStartLocal(event.target.value)}
          required
          type="datetime-local"
          value={startLocal}
        />
        <FieldError
          id="startAt-error"
          message={
            state.fieldErrors?.startAt ??
            state.fieldErrors?.endAt ??
            conversion.error
          }
        />
        <p className="text-muted-foreground text-xs">
          Saat dilimi: {timeZone}. Bitiş, hizmet süresine göre otomatik
          hesaplanır.
        </p>
      </div>

      {customer && !customer.email ? (
        <p className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
          Bu müşterinin e-posta adresi yok. Randevu oluşturulur ancak e-posta
          hatırlatması gönderilmez.
        </p>
      ) : null}

      <div className="space-y-2">
        <Label htmlFor="technicianUserId">Teknisyen (opsiyonel)</Label>
        <select
          className="bg-background h-10 w-full rounded-md border px-3 text-sm"
          id="technicianUserId"
          name="technicianUserId"
          defaultValue={state.values?.technicianUserId ?? ""}
        >
          <option value="">Daha sonra ata</option>
          {technicians.map((technician) => (
            <option key={technician.id} value={technician.id}>
              {technician.firstName} {technician.lastName}
            </option>
          ))}
        </select>
      </div>

      <div className="bg-muted/40 rounded-lg border p-4 text-sm">
        <CalendarClock className="text-primary mr-2 inline size-4" />
        Tahmini süre: <strong>{duration} dakika</strong>
      </div>

      <PendingButton
        className="w-full"
        disabled={!customers.length || !services.length || !instants}
        pendingLabel="Oluşturuluyor…"
        type="submit"
      >
        Randevuyu oluştur
      </PendingButton>
    </form>
  );
}
